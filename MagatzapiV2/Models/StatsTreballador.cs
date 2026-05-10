using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MagatzapiV2.Models;

[Table("stats_treballador")]
[Index("IdUsuari", Name = "id_usuari_UNIQUE", IsUnique = true)]
public partial class StatsTreballador
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("id_usuari")]
    public int IdUsuari { get; set; }

    [Column("tasques_realitzades")]
    public int TasquesRealitzades { get; set; }

    [Column("total_errors_greus")]
    public int TotalErrorsGreus { get; set; }

    [Column("total_errors_lleus")]
    public int TotalErrorsLleus { get; set; }

    [Column("data_ultim_registre", TypeName = "datetime")]
    public DateTime DataUltimRegistre { get; set; }

    [Column("mitjana_minuts_preparacio_per_tasca")]
    public int? MitjanaMinutsPreparacioPerTasca { get; set; }

    [Column("mitjana_unitats_per_tasca")]
    public int? MitjanaUnitatsPerTasca { get; set; }

    [Column("hora_mitjana_fixatge_entrant", TypeName = "time")]
    public TimeOnly? HoraMitjanaFixatgeEntrant { get; set; }

    [Column("hora_mitjana_fitxatge_sortint", TypeName = "time")]
    public TimeOnly? HoraMitjanaFitxatgeSortint { get; set; }

    [Column("minuts_emprats_descans")]
    public int? MinutsEmpratsDescans { get; set; }

    [ForeignKey("IdUsuari")]
    [InverseProperty("StatsTreballador")]
    public virtual Usuaris IdUsuariNavigation { get; set; } = null!;
}
