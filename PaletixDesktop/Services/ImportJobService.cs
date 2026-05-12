using PaletixDesktop.ViewModels;
using System;
using System.Threading.Tasks;

namespace PaletixDesktop.Services
{
    public sealed class ImportJobService : ViewModelBase
    {
        private bool _isActive;
        private bool _isPaused;
        private bool _isCancellationRequested;
        private string _title = "";
        private string _statusText = "";
        private int _processedCount;
        private int _totalCount;

        public bool IsActive
        {
            get => _isActive;
            private set
            {
                if (SetProperty(ref _isActive, value))
                {
                    OnPropertyChanged(nameof(IsIdle));
                }
            }
        }

        public bool IsIdle => !IsActive;

        public bool IsPaused
        {
            get => _isPaused;
            private set
            {
                if (SetProperty(ref _isPaused, value))
                {
                    OnPropertyChanged(nameof(PauseResumeGlyph));
                    OnPropertyChanged(nameof(PauseResumeTooltip));
                }
            }
        }

        public bool IsCancellationRequested
        {
            get => _isCancellationRequested;
            private set => SetProperty(ref _isCancellationRequested, value);
        }

        public string Title
        {
            get => _title;
            private set => SetProperty(ref _title, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public int ProcessedCount
        {
            get => _processedCount;
            private set
            {
                if (SetProperty(ref _processedCount, value))
                {
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            private set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        public string ProgressText => TotalCount <= 0 ? "" : $"{ProcessedCount}/{TotalCount}";
        public string PauseResumeGlyph => IsPaused ? "\uE768" : "\uE769";
        public string PauseResumeTooltip => IsPaused ? "Reprendre importacio" : "Pausar importacio";

        public void Start(string title, int totalCount)
        {
            Title = title;
            TotalCount = totalCount;
            ProcessedCount = 0;
            StatusText = "Preparant importacio...";
            IsCancellationRequested = false;
            IsPaused = false;
            IsActive = true;
        }

        public void Report(int processedCount, string statusText)
        {
            ProcessedCount = processedCount;
            StatusText = statusText;
        }

        public void TogglePause()
        {
            if (!IsActive || IsCancellationRequested)
            {
                return;
            }

            IsPaused = !IsPaused;
            StatusText = IsPaused ? "Importacio pausada." : "Importacio represa.";
        }

        public void RequestCancel()
        {
            if (!IsActive)
            {
                return;
            }

            IsCancellationRequested = true;
            IsPaused = false;
            StatusText = "Cancel.lant importacio...";
        }

        public async Task WaitIfPausedAsync()
        {
            while (IsActive && IsPaused && !IsCancellationRequested)
            {
                await Task.Delay(150);
            }

            if (IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
        }

        public void Complete(string statusText)
        {
            StatusText = statusText;
            IsPaused = false;
            IsCancellationRequested = false;
            IsActive = false;
        }

        public void Fail(string statusText)
        {
            StatusText = statusText;
            IsPaused = false;
            IsCancellationRequested = false;
            IsActive = false;
        }
    }
}
