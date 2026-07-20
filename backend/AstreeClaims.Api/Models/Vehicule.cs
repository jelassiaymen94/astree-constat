using System;
using System.Collections.Generic;

namespace AstreeClaims.Api.Models;

public partial class Vehicule
{
    public string VehicleId { get; set; } = null!;

    public string ContractId { get; set; } = null!;

    public string TypeVehicule { get; set; } = null!;

    public string Marque { get; set; } = null!;

    public string Modele { get; set; } = null!;

    public string Immatriculation { get; set; } = null!;

    public virtual Contrat Contract { get; set; } = null!;

    public virtual ICollection<Sinistre> Sinistres { get; set; } = new List<Sinistre>();
}
