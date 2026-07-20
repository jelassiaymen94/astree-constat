using System;
using System.Collections.Generic;

namespace AstreeClaims.Api.Models;

public partial class Client
{
    public string ClientId { get; set; } = null!;

    public string Nom { get; set; } = null!;

    public string Prenom { get; set; } = null!;

    public string Gouvernorat { get; set; } = null!;

    public virtual ICollection<Contrat> Contrats { get; set; } = new List<Contrat>();

    public virtual ICollection<Sinistre> Sinistres { get; set; } = new List<Sinistre>();
}
