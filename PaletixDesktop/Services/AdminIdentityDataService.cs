using PaletixDesktop.Models;
using PaletixDesktop.Settings;
using SharedContracts.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PaletixDesktop.Services
{
    public sealed class AdminIdentityDataService
    {
        public const string UsersEntityName = "admin_users";
        public const string RolesEntityName = "admin_roles";
        public const string JobTitlesEntityName = "admin_carrecs";

        private const string UsersEndpoint = "api/Usuaris";
        private const string RolesEndpoint = "api/Rols";
        private const string JobTitlesEndpoint = "api/Carrecs";

        private const string UsersCacheKey = "admin/users/list/v1";
        private const string RolesCacheKey = "admin/roles/list/v1";
        private const string JobTitlesCacheKey = "admin/carrecs/list/v1";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly EntityApiService<UsuarisReadDto, UsuarisRequestDto> _usersApi;
        private readonly EntityApiService<RolsReadDto, RolsRequestDto> _rolesApi;
        private readonly EntityApiService<CarrecsReadDto, CarrecsRequestDto> _jobTitlesApi;
        private readonly LocalDatabase _localDatabase;
        private readonly SyncQueue _syncQueue;

        public AdminIdentityDataService(ApiClient apiClient, LocalDatabase localDatabase, SyncQueue syncQueue)
        {
            _usersApi = new EntityApiService<UsuarisReadDto, UsuarisRequestDto>(apiClient, UsersEndpoint);
            _rolesApi = new EntityApiService<RolsReadDto, RolsRequestDto>(apiClient, RolesEndpoint);
            _jobTitlesApi = new EntityApiService<CarrecsReadDto, CarrecsRequestDto>(apiClient, JobTitlesEndpoint);
            _localDatabase = localDatabase;
            _syncQueue = syncQueue;
        }

        public async Task<AdminIdentityLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        {
            await _localDatabase.InitializeAsync(cancellationToken);

            if (!await CheckConnectionAsync(cancellationToken))
            {
                return await LoadCachedAsync("Administracio carregada des de SQLite. API no disponible.", cancellationToken);
            }

            var errors = 0;
            errors += await SynchronizeSimplePendingAsync(RolesEntityName, RolesEndpoint, _rolesApi, RolesCacheKey, RoleFromRequest, cancellationToken);
            errors += await SynchronizeSimplePendingAsync(JobTitlesEntityName, JobTitlesEndpoint, _jobTitlesApi, JobTitlesCacheKey, JobTitleFromRequest, cancellationToken);
            errors += await SynchronizeUsersPendingAsync(cancellationToken);

            try
            {
                var roles = await LoadRolesFromApiAsync(cancellationToken);
                var jobTitles = await LoadJobTitlesFromApiAsync(cancellationToken);
                var users = await LoadUsersFromApiAsync(roles, jobTitles, cancellationToken);

                ApplySimpleActiveOutbox<RolsRequestDto>(roles, await ReadRolesCacheAsync(cancellationToken), await _syncQueue.GetActiveAsync(RolesEntityName, cancellationToken), RoleFromRequest);
                ApplySimpleActiveOutbox<CarrecsRequestDto>(jobTitles, await ReadJobTitlesCacheAsync(cancellationToken), await _syncQueue.GetActiveAsync(JobTitlesEntityName, cancellationToken), JobTitleFromRequest);
                ApplyUserActiveOutbox(users, await ReadUsersCacheAsync(cancellationToken), await _syncQueue.GetActiveAsync(UsersEntityName, cancellationToken), roles, jobTitles);

                await SaveRolesCacheAsync(roles, cancellationToken);
                await SaveJobTitlesCacheAsync(jobTitles, cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);

                var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
                var message = errors > 0
                    ? $"API carregada amb {errors} error(s) de sincronitzacio d'administracio."
                    : pending > 0
                        ? $"API carregada. {pending} canvi(s) pendents."
                        : "Administracio carregada des de l'API.";

                return new AdminIdentityLoadResult(users, roles, jobTitles, true, pending, message);
            }
            catch (Exception ex)
            {
                return await LoadCachedAsync($"Administracio carregada des de SQLite. Error API: {ex.Message}", cancellationToken);
            }
        }

        public async Task<AdminIdentityLoadResult> LoadCachedAsync(CancellationToken cancellationToken = default)
        {
            return await LoadCachedAsync("Administracio carregada des de SQLite.", cancellationToken);
        }

        public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(AppConstants.ConnectionCheckTimeoutSeconds));
                await _usersApi.GetPageAsync(1, 1, timeout.Token);
                return true;
            }
            catch (ApiException)
            {
                return true;
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                return false;
            }
        }

        public async Task<AdminIdentityMutationResult> CreateUserAsync(AdminUserRecord draft, IReadOnlyList<AdminUserRecord> current, CancellationToken cancellationToken = default)
        {
            var request = ToUserRequest(draft);
            var users = current.Select(CloneUser).ToList();

            try
            {
                var created = await _usersApi.CreateAsync(request, cancellationToken);
                var record = ToUserRecord(created, AdminIdentitySyncState.Synced, "Sincronitzat");
                users.Insert(0, record);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(record, users, "Usuari creat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var record = UserFromRequest(NextUserTemporaryId(users), request, AdminIdentitySyncState.Pending, "Creacio pendent");
                users.Insert(0, record);
                await _syncQueue.UpsertAsync(UsersEntityName, record.IdText, "POST", UsersEndpoint, request, "Pending", cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(record, users, "Usuari creat offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                var record = UserFromRequest(NextUserTemporaryId(users), request, AdminIdentitySyncState.Error, ex.Message);
                users.Insert(0, record);
                await _syncQueue.UpsertAsync(UsersEntityName, record.IdText, "POST", UsersEndpoint, request, "Error", cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(record, users, "L'usuari queda en error sync.", cancellationToken);
            }
        }

        public async Task<AdminIdentityMutationResult> UpdateUserAsync(AdminUserRecord draft, IReadOnlyList<AdminUserRecord> current, CancellationToken cancellationToken = default)
        {
            var request = ToUserRequest(draft);
            var users = current.Select(CloneUser).ToList();

            if (draft.Id < 0)
            {
                draft.SyncState = AdminIdentitySyncState.Pending;
                draft.SyncMessage = "Creacio pendent actualitzada";
                ReplaceUser(users, draft);
                await _syncQueue.UpsertAsync(UsersEntityName, draft.IdText, "POST", UsersEndpoint, request, "Pending", cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(draft, users, "Usuari pendent actualitzat.", cancellationToken);
            }

            try
            {
                await _usersApi.UpdateAsync(draft.Id, request, cancellationToken);
                draft.SyncState = AdminIdentitySyncState.Synced;
                draft.SyncMessage = "Sincronitzat";
                ReplaceUser(users, draft);
                await _syncQueue.RemoveForEntityAsync(UsersEntityName, draft.IdText, cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(draft, users, "Usuari actualitzat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                draft.SyncState = AdminIdentitySyncState.Pending;
                draft.SyncMessage = "Edicio pendent";
                ReplaceUser(users, draft);
                await _syncQueue.UpsertAsync(UsersEntityName, draft.IdText, "PUT", $"{UsersEndpoint}/{draft.Id}", request, "Pending", cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(draft, users, "Usuari actualitzat offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                draft.SyncState = AdminIdentitySyncState.Error;
                draft.SyncMessage = ex.Message;
                ReplaceUser(users, draft);
                await _syncQueue.UpsertAsync(UsersEntityName, draft.IdText, "PUT", $"{UsersEndpoint}/{draft.Id}", request, "Error", cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(draft, users, "L'edicio queda en error sync.", cancellationToken);
            }
        }

        public async Task<AdminIdentityMutationResult> DeleteUserAsync(AdminUserRecord user, IReadOnlyList<AdminUserRecord> current, CancellationToken cancellationToken = default)
        {
            var users = current.Select(CloneUser).ToList();
            if (user.Id < 0)
            {
                users.RemoveAll(item => item.Id == user.Id);
                await _syncQueue.RemoveForEntityAsync(UsersEntityName, user.IdText, cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(null, users, "Usuari pendent descartat.", cancellationToken);
            }

            try
            {
                await _usersApi.DeleteAsync(user.Id, cancellationToken);
                users.RemoveAll(item => item.Id == user.Id);
                await _syncQueue.RemoveForEntityAsync(UsersEntityName, user.IdText, cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(null, users, "Usuari eliminat.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                user.SyncState = AdminIdentitySyncState.PendingDelete;
                user.SyncMessage = "Eliminacio pendent";
                ReplaceUser(users, user);
                await _syncQueue.UpsertAsync(UsersEntityName, user.IdText, "DELETE", $"{UsersEndpoint}/{user.Id}", new { user.Id }, "Pending", cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(user, users, "Usuari marcat per eliminar offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                user.SyncState = AdminIdentitySyncState.Error;
                user.SyncMessage = ex.Message;
                ReplaceUser(users, user);
                await _syncQueue.UpsertAsync(UsersEntityName, user.IdText, "DELETE", $"{UsersEndpoint}/{user.Id}", new { user.Id }, "Error", cancellationToken);
                await SaveUsersCacheAsync(users, cancellationToken);
                return await UserResultAsync(user, users, "L'eliminacio queda en error sync.", cancellationToken);
            }
        }

        public Task<AdminSimpleMutationResult> CreateRoleAsync(AdminSimpleRecord draft, IReadOnlyList<AdminSimpleRecord> current, CancellationToken cancellationToken = default) =>
            CreateSimpleAsync(draft, current, _rolesApi, RolesEntityName, RolesEndpoint, RolesCacheKey, ToRoleRequest, RoleFromRequest, SaveRolesCacheAsync, cancellationToken);

        public Task<AdminSimpleMutationResult> UpdateRoleAsync(AdminSimpleRecord draft, IReadOnlyList<AdminSimpleRecord> current, CancellationToken cancellationToken = default) =>
            UpdateSimpleAsync(draft, current, _rolesApi, RolesEntityName, RolesEndpoint, RolesCacheKey, ToRoleRequest, SaveRolesCacheAsync, cancellationToken);

        public Task<AdminSimpleMutationResult> DeleteRoleAsync(AdminSimpleRecord role, IReadOnlyList<AdminSimpleRecord> current, CancellationToken cancellationToken = default) =>
            DeleteSimpleAsync(role, current, _rolesApi, RolesEntityName, RolesEndpoint, RolesCacheKey, SaveRolesCacheAsync, cancellationToken);

        public Task<AdminSimpleMutationResult> CreateJobTitleAsync(AdminSimpleRecord draft, IReadOnlyList<AdminSimpleRecord> current, CancellationToken cancellationToken = default) =>
            CreateSimpleAsync(draft, current, _jobTitlesApi, JobTitlesEntityName, JobTitlesEndpoint, JobTitlesCacheKey, ToJobTitleRequest, JobTitleFromRequest, SaveJobTitlesCacheAsync, cancellationToken);

        public Task<AdminSimpleMutationResult> UpdateJobTitleAsync(AdminSimpleRecord draft, IReadOnlyList<AdminSimpleRecord> current, CancellationToken cancellationToken = default) =>
            UpdateSimpleAsync(draft, current, _jobTitlesApi, JobTitlesEntityName, JobTitlesEndpoint, JobTitlesCacheKey, ToJobTitleRequest, SaveJobTitlesCacheAsync, cancellationToken);

        public Task<AdminSimpleMutationResult> DeleteJobTitleAsync(AdminSimpleRecord jobTitle, IReadOnlyList<AdminSimpleRecord> current, CancellationToken cancellationToken = default) =>
            DeleteSimpleAsync(jobTitle, current, _jobTitlesApi, JobTitlesEntityName, JobTitlesEndpoint, JobTitlesCacheKey, SaveJobTitlesCacheAsync, cancellationToken);

        private async Task<AdminIdentityLoadResult> LoadCachedAsync(string message, CancellationToken cancellationToken)
        {
            var roles = await ReadRolesCacheAsync(cancellationToken) ?? new List<AdminSimpleRecord>();
            var jobTitles = await ReadJobTitlesCacheAsync(cancellationToken) ?? new List<AdminSimpleRecord>();
            var users = await ReadUsersCacheAsync(cancellationToken) ?? new List<AdminUserRecord>();

            ApplySimpleActiveOutbox<RolsRequestDto>(roles, roles, await _syncQueue.GetActiveAsync(RolesEntityName, cancellationToken), RoleFromRequest);
            ApplySimpleActiveOutbox<CarrecsRequestDto>(jobTitles, jobTitles, await _syncQueue.GetActiveAsync(JobTitlesEntityName, cancellationToken), JobTitleFromRequest);
            ApplyUserActiveOutbox(users, users, await _syncQueue.GetActiveAsync(UsersEntityName, cancellationToken), roles, jobTitles);
            RelabelUsers(users, roles, jobTitles);

            await SaveRolesCacheAsync(roles, cancellationToken);
            await SaveJobTitlesCacheAsync(jobTitles, cancellationToken);
            await SaveUsersCacheAsync(users, cancellationToken);

            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            var finalMessage = users.Count == 0 && roles.Count == 0 && jobTitles.Count == 0
                ? "SQLite encara no te dades locals d'administracio."
                : pending > 0
                    ? $"{message} {pending} canvi(s) pendents."
                    : message;

            return new AdminIdentityLoadResult(users, roles, jobTitles, false, pending, finalMessage);
        }

        private async Task<List<AdminUserRecord>> LoadUsersFromApiAsync(IReadOnlyList<AdminSimpleRecord> roles, IReadOnlyList<AdminSimpleRecord> jobTitles, CancellationToken cancellationToken)
        {
            var first = await _usersApi.GetPageAsync(1, 100, cancellationToken);
            var users = first.Items.Select(item => ToUserRecord(item, AdminIdentitySyncState.Synced, "Sincronitzat")).ToList();
            for (var page = 2; page <= first.TotalPages; page++)
            {
                var next = await _usersApi.GetPageAsync(page, 100, cancellationToken);
                users.AddRange(next.Items.Select(item => ToUserRecord(item, AdminIdentitySyncState.Synced, "Sincronitzat")));
            }

            RelabelUsers(users, roles, jobTitles);
            return users;
        }

        private async Task<List<AdminSimpleRecord>> LoadRolesFromApiAsync(CancellationToken cancellationToken)
        {
            var first = await _rolesApi.GetPageAsync(1, 100, cancellationToken);
            var items = first.Items.Select(ToRoleRecord).ToList();
            for (var page = 2; page <= first.TotalPages; page++)
            {
                var next = await _rolesApi.GetPageAsync(page, 100, cancellationToken);
                items.AddRange(next.Items.Select(ToRoleRecord));
            }

            return items;
        }

        private async Task<List<AdminSimpleRecord>> LoadJobTitlesFromApiAsync(CancellationToken cancellationToken)
        {
            var first = await _jobTitlesApi.GetPageAsync(1, 100, cancellationToken);
            var items = first.Items.Select(ToJobTitleRecord).ToList();
            for (var page = 2; page <= first.TotalPages; page++)
            {
                var next = await _jobTitlesApi.GetPageAsync(page, 100, cancellationToken);
                items.AddRange(next.Items.Select(ToJobTitleRecord));
            }

            return items;
        }

        private async Task<int> SynchronizeUsersPendingAsync(CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingAsync(UsersEntityName, cancellationToken);
            var cached = await ReadUsersCacheAsync(cancellationToken) ?? new List<AdminUserRecord>();
            var errors = 0;
            foreach (var item in pending)
            {
                try
                {
                    if (item.Method == "POST")
                    {
                        var request = DeserializePayload<UsuarisRequestDto>(item);
                        var created = await _usersApi.CreateAsync(request, cancellationToken);
                        cached.RemoveAll(user => user.IdText == item.EntityId);
                        cached.Insert(0, ToUserRecord(created, AdminIdentitySyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                    {
                        var request = DeserializePayload<UsuarisRequestDto>(item);
                        await _usersApi.UpdateAsync(updateId, request, cancellationToken);
                        ReplaceUser(cached, UserFromRequest(updateId, request, AdminIdentitySyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                    {
                        await _usersApi.DeleteAsync(deleteId, cancellationToken);
                        cached.RemoveAll(user => user.Id == deleteId);
                    }

                    await _syncQueue.MarkCompletedAsync(item.Id, cancellationToken);
                }
                catch (Exception ex) when (IsConnectivityFailure(ex))
                {
                    break;
                }
                catch (Exception)
                {
                    await _syncQueue.MarkFailedAsync(item.Id, cancellationToken);
                    MarkUserCachedError(cached, item);
                    errors++;
                }
            }

            await SaveUsersCacheAsync(cached, cancellationToken);
            return errors;
        }

        private async Task<int> SynchronizeSimplePendingAsync<TRequest, TRead>(
            string entityName,
            string endpoint,
            EntityApiService<TRead, TRequest> api,
            string cacheKey,
            Func<int, TRequest, AdminIdentitySyncState, string, AdminSimpleRecord> fromRequest,
            CancellationToken cancellationToken)
            where TRead : class
            where TRequest : class
        {
            var pending = await _syncQueue.GetPendingAsync(entityName, cancellationToken);
            var cached = await _localDatabase.GetJsonAsync<List<AdminSimpleRecord>>(cacheKey, cancellationToken) ?? new List<AdminSimpleRecord>();
            var errors = 0;
            foreach (var item in pending)
            {
                try
                {
                    if (item.Method == "POST")
                    {
                        var request = DeserializePayload<TRequest>(item);
                        var created = await api.CreateAsync(request, cancellationToken);
                        cached.RemoveAll(record => record.IdText == item.EntityId);
                        cached.Insert(0, created switch
                        {
                            RolsReadDto role => ToRoleRecord(role),
                            CarrecsReadDto jobTitle => ToJobTitleRecord(jobTitle),
                            _ => throw new InvalidOperationException("Tipus d'administracio no suportat.")
                        });
                    }
                    else if (item.Method == "PUT" && int.TryParse(item.EntityId, out var updateId))
                    {
                        var request = DeserializePayload<TRequest>(item);
                        await api.UpdateAsync(updateId, request, cancellationToken);
                        ReplaceSimple(cached, fromRequest(updateId, request, AdminIdentitySyncState.Synced, "Sincronitzat"));
                    }
                    else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                    {
                        await api.DeleteAsync(deleteId, cancellationToken);
                        cached.RemoveAll(record => record.Id == deleteId);
                    }

                    await _syncQueue.MarkCompletedAsync(item.Id, cancellationToken);
                }
                catch (Exception ex) when (IsConnectivityFailure(ex))
                {
                    break;
                }
                catch (Exception)
                {
                    await _syncQueue.MarkFailedAsync(item.Id, cancellationToken);
                    MarkSimpleCachedError(cached, item);
                    errors++;
                }
            }

            await _localDatabase.SetJsonAsync(cacheKey, cached, cancellationToken);
            return errors;
        }

        private async Task<AdminSimpleMutationResult> CreateSimpleAsync<TRead, TRequest>(
            AdminSimpleRecord draft,
            IReadOnlyList<AdminSimpleRecord> current,
            EntityApiService<TRead, TRequest> api,
            string entityName,
            string endpoint,
            string cacheKey,
            Func<AdminSimpleRecord, TRequest> toRequest,
            Func<int, TRequest, AdminIdentitySyncState, string, AdminSimpleRecord> fromRequest,
            Func<IReadOnlyList<AdminSimpleRecord>, CancellationToken, Task> saveCache,
            CancellationToken cancellationToken)
            where TRead : class
            where TRequest : class
        {
            var request = toRequest(draft);
            var records = current.Select(CloneSimple).ToList();
            try
            {
                var created = await api.CreateAsync(request, cancellationToken);
                var record = created switch
                {
                    RolsReadDto role => ToRoleRecord(role),
                    CarrecsReadDto jobTitle => ToJobTitleRecord(jobTitle),
                    _ => throw new InvalidOperationException("Tipus d'administracio no suportat.")
                };
                records.Insert(0, record);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(record, records, "Registre creat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                var record = fromRequest(NextSimpleTemporaryId(records), request, AdminIdentitySyncState.Pending, "Creacio pendent");
                records.Insert(0, record);
                await _syncQueue.UpsertAsync(entityName, record.IdText, "POST", endpoint, request, "Pending", cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(record, records, "Registre creat offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                var record = fromRequest(NextSimpleTemporaryId(records), request, AdminIdentitySyncState.Error, ex.Message);
                records.Insert(0, record);
                await _syncQueue.UpsertAsync(entityName, record.IdText, "POST", endpoint, request, "Error", cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(record, records, "El registre queda en error sync.", cancellationToken);
            }
        }

        private async Task<AdminSimpleMutationResult> UpdateSimpleAsync<TRead, TRequest>(
            AdminSimpleRecord draft,
            IReadOnlyList<AdminSimpleRecord> current,
            EntityApiService<TRead, TRequest> api,
            string entityName,
            string endpoint,
            string cacheKey,
            Func<AdminSimpleRecord, TRequest> toRequest,
            Func<IReadOnlyList<AdminSimpleRecord>, CancellationToken, Task> saveCache,
            CancellationToken cancellationToken)
            where TRead : class
            where TRequest : class
        {
            var request = toRequest(draft);
            var records = current.Select(CloneSimple).ToList();
            if (draft.Id < 0)
            {
                draft.SyncState = AdminIdentitySyncState.Pending;
                draft.SyncMessage = "Creacio pendent actualitzada";
                ReplaceSimple(records, draft);
                await _syncQueue.UpsertAsync(entityName, draft.IdText, "POST", endpoint, request, "Pending", cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(draft, records, "Registre pendent actualitzat.", cancellationToken);
            }

            try
            {
                await api.UpdateAsync(draft.Id, request, cancellationToken);
                draft.SyncState = AdminIdentitySyncState.Synced;
                draft.SyncMessage = "Sincronitzat";
                ReplaceSimple(records, draft);
                await _syncQueue.RemoveForEntityAsync(entityName, draft.IdText, cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(draft, records, "Registre actualitzat a l'API.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                draft.SyncState = AdminIdentitySyncState.Pending;
                draft.SyncMessage = "Edicio pendent";
                ReplaceSimple(records, draft);
                await _syncQueue.UpsertAsync(entityName, draft.IdText, "PUT", $"{endpoint}/{draft.Id}", request, "Pending", cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(draft, records, "Registre actualitzat offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                draft.SyncState = AdminIdentitySyncState.Error;
                draft.SyncMessage = ex.Message;
                ReplaceSimple(records, draft);
                await _syncQueue.UpsertAsync(entityName, draft.IdText, "PUT", $"{endpoint}/{draft.Id}", request, "Error", cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(draft, records, "L'edicio queda en error sync.", cancellationToken);
            }
        }

        private async Task<AdminSimpleMutationResult> DeleteSimpleAsync<TRead, TRequest>(
            AdminSimpleRecord record,
            IReadOnlyList<AdminSimpleRecord> current,
            EntityApiService<TRead, TRequest> api,
            string entityName,
            string endpoint,
            string cacheKey,
            Func<IReadOnlyList<AdminSimpleRecord>, CancellationToken, Task> saveCache,
            CancellationToken cancellationToken)
            where TRead : class
            where TRequest : class
        {
            var records = current.Select(CloneSimple).ToList();
            if (record.Id < 0)
            {
                records.RemoveAll(item => item.Id == record.Id);
                await _syncQueue.RemoveForEntityAsync(entityName, record.IdText, cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(null, records, "Registre pendent descartat.", cancellationToken);
            }

            try
            {
                await api.DeleteAsync(record.Id, cancellationToken);
                records.RemoveAll(item => item.Id == record.Id);
                await _syncQueue.RemoveForEntityAsync(entityName, record.IdText, cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(null, records, "Registre eliminat.", cancellationToken);
            }
            catch (Exception ex) when (IsConnectivityFailure(ex))
            {
                record.SyncState = AdminIdentitySyncState.PendingDelete;
                record.SyncMessage = "Eliminacio pendent";
                ReplaceSimple(records, record);
                await _syncQueue.UpsertAsync(entityName, record.IdText, "DELETE", $"{endpoint}/{record.Id}", new { record.Id }, "Pending", cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(record, records, "Registre marcat per eliminar offline.", cancellationToken);
            }
            catch (Exception ex)
            {
                record.SyncState = AdminIdentitySyncState.Error;
                record.SyncMessage = ex.Message;
                ReplaceSimple(records, record);
                await _syncQueue.UpsertAsync(entityName, record.IdText, "DELETE", $"{endpoint}/{record.Id}", new { record.Id }, "Error", cancellationToken);
                await saveCache(records, cancellationToken);
                return await SimpleResultAsync(record, records, "L'eliminacio queda en error sync.", cancellationToken);
            }
        }

        private async Task<List<AdminUserRecord>?> ReadUsersCacheAsync(CancellationToken cancellationToken) =>
            await _localDatabase.GetJsonAsync<List<AdminUserRecord>>(UsersCacheKey, cancellationToken);

        private async Task<List<AdminSimpleRecord>?> ReadRolesCacheAsync(CancellationToken cancellationToken) =>
            await _localDatabase.GetJsonAsync<List<AdminSimpleRecord>>(RolesCacheKey, cancellationToken);

        private async Task<List<AdminSimpleRecord>?> ReadJobTitlesCacheAsync(CancellationToken cancellationToken) =>
            await _localDatabase.GetJsonAsync<List<AdminSimpleRecord>>(JobTitlesCacheKey, cancellationToken);

        public Task SaveUsersCacheAsync(IReadOnlyList<AdminUserRecord> users, CancellationToken cancellationToken) =>
            SaveCacheAsync(UsersCacheKey, users.Select(CloneUser).ToList(), cancellationToken);

        public Task SaveRolesCacheAsync(IReadOnlyList<AdminSimpleRecord> roles, CancellationToken cancellationToken) =>
            SaveCacheAsync(RolesCacheKey, roles.Select(CloneSimple).ToList(), cancellationToken);

        public Task SaveJobTitlesCacheAsync(IReadOnlyList<AdminSimpleRecord> jobTitles, CancellationToken cancellationToken) =>
            SaveCacheAsync(JobTitlesCacheKey, jobTitles.Select(CloneSimple).ToList(), cancellationToken);

        private async Task SaveCacheAsync<T>(string key, List<T> records, CancellationToken cancellationToken)
            where T : AdminIdentityRecordBase
        {
            foreach (var record in records)
            {
                record.IsSelected = false;
            }

            await _localDatabase.SetJsonAsync(key, records, cancellationToken);
        }

        private async Task<AdminIdentityMutationResult> UserResultAsync(AdminUserRecord? user, IReadOnlyList<AdminUserRecord> users, string message, CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            return new AdminIdentityMutationResult(user, users, pending, message);
        }

        private async Task<AdminSimpleMutationResult> SimpleResultAsync(AdminSimpleRecord? record, IReadOnlyList<AdminSimpleRecord> records, string message, CancellationToken cancellationToken)
        {
            var pending = await _syncQueue.GetPendingCountAsync(cancellationToken);
            return new AdminSimpleMutationResult(record, records, pending, message);
        }

        private static UsuarisRequestDto ToUserRequest(AdminUserRecord user) => new()
        {
            Nom = user.Nom.Trim(),
            Cognoms = user.Cognoms.Trim(),
            Dni = user.Dni.Trim().ToUpperInvariant(),
            DataNaixement = DateOnly.FromDateTime(user.DataNaixement),
            DataContractacio = DateOnly.FromDateTime(user.DataContractacio),
            Email = user.Email.Trim(),
            Telefon = user.Telefon.Trim(),
            Password = user.Password,
            Salari = user.Salari,
            Torn = user.Torn,
            NumSeguretatSocial = EmptyToNull(user.NumSeguretatSocial),
            NumCompteBancari = EmptyToNull(user.NumCompteBancari),
            IdCarrec = user.IdCarrec,
            IdRol = user.IdRol,
            SaldoPunts = user.SaldoPunts,
            Nivell = user.Nivell,
            AnysExperiencia = user.AnysExperiencia
        };

        private static AdminUserRecord ToUserRecord(UsuarisReadDto dto, AdminIdentitySyncState state, string message) => new()
        {
            Id = dto.Id,
            Nom = dto.Nom,
            Cognoms = dto.Cognoms,
            Dni = dto.Dni,
            DataNaixement = dto.DataNaixement.ToDateTime(TimeOnly.MinValue),
            DataContractacio = dto.DataContractacio.ToDateTime(TimeOnly.MinValue),
            Email = dto.Email,
            Telefon = dto.Telefon,
            Salari = dto.Salari,
            Torn = dto.Torn,
            NumSeguretatSocial = dto.NumSeguretatSocial,
            NumCompteBancari = dto.NumCompteBancari,
            IdCarrec = dto.IdCarrec,
            IdRol = dto.IdRol,
            SaldoPunts = dto.SaldoPunts,
            Nivell = dto.Nivell,
            AnysExperiencia = dto.AnysExperiencia,
            DataDeCreacio = dto.DataDeCreacio?.ToDateTime(TimeOnly.MinValue),
            SyncState = state,
            SyncMessage = message
        };

        private static AdminUserRecord UserFromRequest(int id, UsuarisRequestDto request, AdminIdentitySyncState state, string message) => new()
        {
            Id = id,
            Nom = request.Nom,
            Cognoms = request.Cognoms,
            Dni = request.Dni,
            DataNaixement = request.DataNaixement.ToDateTime(TimeOnly.MinValue),
            DataContractacio = request.DataContractacio.ToDateTime(TimeOnly.MinValue),
            Email = request.Email,
            Telefon = request.Telefon,
            Password = request.Password,
            Salari = request.Salari,
            Torn = request.Torn,
            NumSeguretatSocial = request.NumSeguretatSocial,
            NumCompteBancari = request.NumCompteBancari,
            IdCarrec = request.IdCarrec,
            IdRol = request.IdRol,
            SaldoPunts = request.SaldoPunts,
            Nivell = request.Nivell,
            AnysExperiencia = request.AnysExperiencia,
            SyncState = state,
            SyncMessage = message
        };

        private static AdminSimpleRecord ToRoleRecord(RolsReadDto dto) => new() { Id = dto.Id, Nom = dto.Nom, SyncState = AdminIdentitySyncState.Synced, SyncMessage = "Sincronitzat" };
        private static AdminSimpleRecord ToJobTitleRecord(CarrecsReadDto dto) => new() { Id = dto.Id, Nom = dto.Nom, SyncState = AdminIdentitySyncState.Synced, SyncMessage = "Sincronitzat" };
        private static RolsRequestDto ToRoleRequest(AdminSimpleRecord record) => new() { Nom = record.Nom.Trim() };
        private static CarrecsRequestDto ToJobTitleRequest(AdminSimpleRecord record) => new() { Nom = record.Nom.Trim() };
        private static AdminSimpleRecord RoleFromRequest(int id, RolsRequestDto request, AdminIdentitySyncState state, string message) => new() { Id = id, Nom = request.Nom, SyncState = state, SyncMessage = message };
        private static AdminSimpleRecord JobTitleFromRequest(int id, CarrecsRequestDto request, AdminIdentitySyncState state, string message) => new() { Id = id, Nom = request.Nom, SyncState = state, SyncMessage = message };

        private static void RelabelUsers(IReadOnlyList<AdminUserRecord> users, IReadOnlyList<AdminSimpleRecord> roles, IReadOnlyList<AdminSimpleRecord> jobTitles)
        {
            foreach (var user in users)
            {
                user.RolText = roles.FirstOrDefault(role => role.Id == user.IdRol)?.Nom ?? $"Rol {user.IdRol}";
                user.CarrecText = jobTitles.FirstOrDefault(jobTitle => jobTitle.Id == user.IdCarrec)?.Nom ?? $"Carrec {user.IdCarrec}";
            }
        }

        private static void ApplyUserActiveOutbox(List<AdminUserRecord> users, IReadOnlyList<AdminUserRecord>? cached, IReadOnlyList<SyncQueueItem> active, IReadOnlyList<AdminSimpleRecord> roles, IReadOnlyList<AdminSimpleRecord> jobTitles)
        {
            foreach (var item in active)
            {
                var state = item.Status == "Error" ? AdminIdentitySyncState.Error : AdminIdentitySyncState.Pending;
                if (item.Method is "POST" or "PUT")
                {
                    var request = DeserializePayload<UsuarisRequestDto>(item);
                    var id = int.TryParse(item.EntityId, out var parsed) ? parsed : -1;
                    ReplaceUser(users, UserFromRequest(id, request, state, item.Status == "Error" ? "Error sync" : "Canvi pendent"));
                }
                else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                {
                    var existing = users.FirstOrDefault(user => user.Id == deleteId) ?? cached?.FirstOrDefault(user => user.Id == deleteId);
                    if (existing is not null)
                    {
                        existing.SyncState = item.Status == "Error" ? AdminIdentitySyncState.Error : AdminIdentitySyncState.PendingDelete;
                        existing.SyncMessage = item.Status == "Error" ? "Error sync" : "Eliminacio pendent";
                        ReplaceUser(users, existing);
                    }
                }
            }

            RelabelUsers(users, roles, jobTitles);
        }

        private static void ApplySimpleActiveOutbox<TRequest>(List<AdminSimpleRecord> records, IReadOnlyList<AdminSimpleRecord>? cached, IReadOnlyList<SyncQueueItem> active, Func<int, TRequest, AdminIdentitySyncState, string, AdminSimpleRecord> fromRequest)
        {
            foreach (var item in active)
            {
                var state = item.Status == "Error" ? AdminIdentitySyncState.Error : AdminIdentitySyncState.Pending;
                if (item.Method is "POST" or "PUT")
                {
                    var request = DeserializePayload<TRequest>(item);
                    var id = int.TryParse(item.EntityId, out var parsed) ? parsed : -1;
                    ReplaceSimple(records, fromRequest(id, request, state, item.Status == "Error" ? "Error sync" : "Canvi pendent"));
                }
                else if (item.Method == "DELETE" && int.TryParse(item.EntityId, out var deleteId))
                {
                    var existing = records.FirstOrDefault(record => record.Id == deleteId) ?? cached?.FirstOrDefault(record => record.Id == deleteId);
                    if (existing is not null)
                    {
                        existing.SyncState = item.Status == "Error" ? AdminIdentitySyncState.Error : AdminIdentitySyncState.PendingDelete;
                        existing.SyncMessage = item.Status == "Error" ? "Error sync" : "Eliminacio pendent";
                        ReplaceSimple(records, existing);
                    }
                }
            }
        }

        private static AdminUserRecord CloneUser(AdminUserRecord user) => new()
        {
            Id = user.Id,
            Nom = user.Nom,
            Cognoms = user.Cognoms,
            Dni = user.Dni,
            DataNaixement = user.DataNaixement,
            DataContractacio = user.DataContractacio,
            Email = user.Email,
            Telefon = user.Telefon,
            Password = user.Password,
            Salari = user.Salari,
            Torn = user.Torn,
            NumSeguretatSocial = user.NumSeguretatSocial,
            NumCompteBancari = user.NumCompteBancari,
            IdCarrec = user.IdCarrec,
            IdRol = user.IdRol,
            SaldoPunts = user.SaldoPunts,
            Nivell = user.Nivell,
            AnysExperiencia = user.AnysExperiencia,
            DataDeCreacio = user.DataDeCreacio,
            CarrecText = user.CarrecText,
            RolText = user.RolText,
            SyncState = user.SyncState,
            SyncMessage = user.SyncMessage
        };

        private static AdminSimpleRecord CloneSimple(AdminSimpleRecord record) => new()
        {
            Id = record.Id,
            Nom = record.Nom,
            SyncState = record.SyncState,
            SyncMessage = record.SyncMessage
        };

        private static void ReplaceUser(List<AdminUserRecord> users, AdminUserRecord user)
        {
            var index = users.FindIndex(item => item.Id == user.Id);
            if (index >= 0)
            {
                users[index] = CloneUser(user);
                return;
            }

            users.Insert(0, CloneUser(user));
        }

        private static void ReplaceSimple(List<AdminSimpleRecord> records, AdminSimpleRecord record)
        {
            var index = records.FindIndex(item => item.Id == record.Id);
            if (index >= 0)
            {
                records[index] = CloneSimple(record);
                return;
            }

            records.Insert(0, CloneSimple(record));
        }

        private static void MarkUserCachedError(List<AdminUserRecord> users, SyncQueueItem item)
        {
            var user = users.FirstOrDefault(record => record.IdText == item.EntityId);
            if (user is not null)
            {
                user.SyncState = AdminIdentitySyncState.Error;
                user.SyncMessage = "Error sync";
            }
        }

        private static void MarkSimpleCachedError(List<AdminSimpleRecord> records, SyncQueueItem item)
        {
            var record = records.FirstOrDefault(entry => entry.IdText == item.EntityId);
            if (record is not null)
            {
                record.SyncState = AdminIdentitySyncState.Error;
                record.SyncMessage = "Error sync";
            }
        }

        private static int NextUserTemporaryId(IReadOnlyList<AdminUserRecord> users)
        {
            var min = users.Count == 0 ? 0 : users.Min(user => user.Id);
            return min < 0 ? min - 1 : -1;
        }

        private static int NextSimpleTemporaryId(IReadOnlyList<AdminSimpleRecord> records)
        {
            var min = records.Count == 0 ? 0 : records.Min(record => record.Id);
            return min < 0 ? min - 1 : -1;
        }

        private static T DeserializePayload<T>(SyncQueueItem item)
        {
            return JsonSerializer.Deserialize<T>(item.Payload, JsonOptions)
                ?? throw new InvalidOperationException("Payload de sincronitzacio invalid.");
        }

        private static bool IsConnectivityFailure(Exception ex) => ex is HttpRequestException or TaskCanceledException;

        private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public sealed record AdminIdentityLoadResult(
        IReadOnlyList<AdminUserRecord> Users,
        IReadOnlyList<AdminSimpleRecord> Roles,
        IReadOnlyList<AdminSimpleRecord> JobTitles,
        bool IsOnline,
        int PendingCount,
        string Message);

    public sealed record AdminIdentityMutationResult(
        AdminUserRecord? User,
        IReadOnlyList<AdminUserRecord> Users,
        int PendingCount,
        string Message);

    public sealed record AdminSimpleMutationResult(
        AdminSimpleRecord? Record,
        IReadOnlyList<AdminSimpleRecord> Records,
        int PendingCount,
        string Message);
}
