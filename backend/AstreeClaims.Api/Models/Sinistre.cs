using System;
using System.Collections.Generic;

namespace AstreeClaims.Api.Models;

public partial class Sinistre
{
    public string ClaimId { get; set; } = null!;

    public string ContractId { get; set; } = null!;

    public string ClientId { get; set; } = null!;

    public string VehicleId { get; set; } = null!;

    public DateOnly DateSinistre { get; set; }

    public string TypeSinistre { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal MontantEstime { get; set; }

    public decimal MontantIndemnisation { get; set; }

    public string Statut { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual Contrat Contract { get; set; } = null!;

    public virtual ICollection<GenerationLog> GenerationLogs { get; set; } = new List<GenerationLog>();

    public virtual Vehicule Vehicle { get; set; } = null!;
}
