using System.Globalization;
using System.Text;
using AstreeClaims.Api.Data;
using AstreeClaims.Api.DTOs.Import;
using AstreeClaims.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace AstreeClaims.Api.Services.Import;

public sealed class DataImportService : IDataImportService
{
    private static readonly string[] ClientHeaders =
        ["ClientId", "Nom", "Prenom", "Gouvernorat"];

    private static readonly string[] ContratHeaders =
        ["ContractId", "ClientId", "TypeCouverture", "DateDebut", "DateFin"];

    private static readonly string[] VehiculeHeaders =
        ["VehicleId", "ContractId", "TypeVehicule", "Marque", "Modele", "Immatriculation"];

    private static readonly string[] SinistreHeaders =
    [
        "ClaimId",
        "ContractId",
        "ClientId",
        "VehicleId",
        "DateSinistre",
        "TypeSinistre",
        "Description",
        "MontantEstime",
        "MontantIndemnisation",
        "Statut"
    ];

    private readonly AstreeClaimsDbContext _dbContext;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(
        AstreeClaimsDbContext dbContext,
        ILogger<DataImportService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ImportResultDto> ImportAsync(
        string sourceDirectory,
        CancellationToken cancellationToken = default)
    {
        var startedAtUtc = DateTime.UtcNow;
        var directory = Path.GetFullPath(sourceDirectory);

        _logger.LogInformation("Starting data import from {SourceDirectory}", directory);

        var clients = ReadClients(Path.Combine(directory, "clients.csv"));
        var contrats = ReadContrats(Path.Combine(directory, "contrats.csv"));
        var vehicules = ReadVehicules(Path.Combine(directory, "vehicules.csv"));
        var sinistres = ReadSinistres(Path.Combine(directory, "sinistres.csv"));

        ValidateSourceData(clients, contrats, vehicules, sinistres);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            cancellationToken);

        var originalAutoDetectChanges = _dbContext.ChangeTracker.AutoDetectChangesEnabled;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var existingClientIds = (await _dbContext.Clients
                .AsNoTracking()
                .Select(client => client.ClientId)
                .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.Ordinal);
            var clientsToInsert = clients
                .Where(client => !existingClientIds.Contains(client.ClientId))
                .ToList();
            _dbContext.Clients.AddRange(clientsToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var existingContractIds = (await _dbContext.Contrats
                .AsNoTracking()
                .Select(contrat => contrat.ContractId)
                .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.Ordinal);
            var contratsToInsert = contrats
                .Where(contrat => !existingContractIds.Contains(contrat.ContractId))
                .ToList();
            _dbContext.Contrats.AddRange(contratsToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var existingVehicleIds = (await _dbContext.Vehicules
                .AsNoTracking()
                .Select(vehicule => vehicule.VehicleId)
                .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.Ordinal);
            var vehiculesToInsert = vehicules
                .Where(vehicule => !existingVehicleIds.Contains(vehicule.VehicleId))
                .ToList();
            _dbContext.Vehicules.AddRange(vehiculesToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var existingClaimIds = (await _dbContext.Sinistres
                .AsNoTracking()
                .Select(sinistre => sinistre.ClaimId)
                .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.Ordinal);
            var sinistresToInsert = sinistres
                .Where(sinistre => !existingClaimIds.Contains(sinistre.ClaimId))
                .ToList();
            _dbContext.Sinistres.AddRange(sinistresToInsert);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var result = new ImportResultDto(
                directory,
                BuildTableResult(clients.Count, clientsToInsert.Count),
                BuildTableResult(contrats.Count, contratsToInsert.Count),
                BuildTableResult(vehicules.Count, vehiculesToInsert.Count),
                BuildTableResult(sinistres.Count, sinistresToInsert.Count),
                startedAtUtc,
                DateTime.UtcNow);

            _logger.LogInformation(
                "Data import completed. Clients={Clients}, Contrats={Contrats}, Vehicules={Vehicules}, Sinistres={Sinistres}",
                result.Clients.InsertedRows,
                result.Contrats.InsertedRows,
                result.Vehicules.InsertedRows,
                result.Sinistres.InsertedRows);

            return result;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(exception, "Data import failed and was rolled back");
            throw;
        }
        finally
        {
            _dbContext.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
        }
    }

    private static ImportTableResultDto BuildTableResult(int sourceRows, int insertedRows) =>
        new(sourceRows, insertedRows, sourceRows - insertedRows);

    private static List<Client> ReadClients(string path) =>
        ReadCsv(path, ClientHeaders)
            .Select((row, index) => new Client
            {
                ClientId = Required(row, "ClientId", path, index),
                Nom = Required(row, "Nom", path, index),
                Prenom = Required(row, "Prenom", path, index),
                Gouvernorat = Required(row, "Gouvernorat", path, index)
            })
            .ToList();

    private static List<Contrat> ReadContrats(string path) =>
        ReadCsv(path, ContratHeaders)
            .Select((row, index) => new Contrat
            {
                ContractId = Required(row, "ContractId", path, index),
                ClientId = Required(row, "ClientId", path, index),
                TypeCouverture = Required(row, "TypeCouverture", path, index),
                DateDebut = ParseDate(row, "DateDebut", path, index),
                DateFin = ParseDate(row, "DateFin", path, index)
            })
            .ToList();

    private static List<Vehicule> ReadVehicules(string path) =>
        ReadCsv(path, VehiculeHeaders)
            .Select((row, index) => new Vehicule
            {
                VehicleId = Required(row, "VehicleId", path, index),
                ContractId = Required(row, "ContractId", path, index),
                TypeVehicule = Required(row, "TypeVehicule", path, index),
                Marque = Required(row, "Marque", path, index),
                Modele = Required(row, "Modele", path, index),
                Immatriculation = Required(row, "Immatriculation", path, index)
            })
            .ToList();

    private static List<Sinistre> ReadSinistres(string path) =>
        ReadCsv(path, SinistreHeaders)
            .Select((row, index) => new Sinistre
            {
                ClaimId = Required(row, "ClaimId", path, index),
                ContractId = Required(row, "ContractId", path, index),
                ClientId = Required(row, "ClientId", path, index),
                VehicleId = Required(row, "VehicleId", path, index),
                DateSinistre = ParseDate(row, "DateSinistre", path, index),
                TypeSinistre = Required(row, "TypeSinistre", path, index),
                Description = Required(row, "Description", path, index),
                MontantEstime = ParseDecimal(row, "MontantEstime", path, index),
                MontantIndemnisation = ParseDecimal(
                    row, "MontantIndemnisation", path, index),
                Statut = Required(row, "Statut", path, index)
            })
            .ToList();

    private static void ValidateSourceData(
        IReadOnlyCollection<Client> clients,
        IReadOnlyCollection<Contrat> contrats,
        IReadOnlyCollection<Vehicule> vehicules,
        IReadOnlyCollection<Sinistre> sinistres)
    {
        EnsureUnique(clients.Select(client => client.ClientId), "ClientId");
        EnsureUnique(contrats.Select(contrat => contrat.ContractId), "ContractId");
        EnsureUnique(vehicules.Select(vehicule => vehicule.VehicleId), "VehicleId");
        EnsureUnique(vehicules.Select(vehicule => vehicule.ContractId), "Vehicule.ContractId");
        EnsureUnique(sinistres.Select(sinistre => sinistre.ClaimId), "ClaimId");

        var clientsById = clients.ToDictionary(client => client.ClientId);
        var contratsById = contrats.ToDictionary(contrat => contrat.ContractId);
        var vehiculesById = vehicules.ToDictionary(vehicule => vehicule.VehicleId);

        foreach (var contrat in contrats)
        {
            if (!clientsById.ContainsKey(contrat.ClientId))
            {
                throw new InvalidDataException(
                    $"Le contrat {contrat.ContractId} référence un client absent.");
            }

            if (contrat.DateFin < contrat.DateDebut)
            {
                throw new InvalidDataException(
                    $"Le contrat {contrat.ContractId} possède une période invalide.");
            }
        }

        foreach (var vehicule in vehicules)
        {
            if (!contratsById.ContainsKey(vehicule.ContractId))
            {
                throw new InvalidDataException(
                    $"Le véhicule {vehicule.VehicleId} référence un contrat absent.");
            }
        }

        foreach (var sinistre in sinistres)
        {
            if (!clientsById.ContainsKey(sinistre.ClientId))
            {
                throw new InvalidDataException(
                    $"Le sinistre {sinistre.ClaimId} référence un client absent.");
            }

            if (!contratsById.TryGetValue(sinistre.ContractId, out var contrat))
            {
                throw new InvalidDataException(
                    $"Le sinistre {sinistre.ClaimId} référence un contrat absent.");
            }

            if (!vehiculesById.TryGetValue(sinistre.VehicleId, out var vehicule))
            {
                throw new InvalidDataException(
                    $"Le sinistre {sinistre.ClaimId} référence un véhicule absent.");
            }

            if (!string.Equals(contrat.ClientId, sinistre.ClientId, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Le client du sinistre {sinistre.ClaimId} ne correspond pas au contrat.");
            }

            if (!string.Equals(vehicule.ContractId, sinistre.ContractId, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Le véhicule du sinistre {sinistre.ClaimId} ne correspond pas au contrat.");
            }

            if (sinistre.DateSinistre < contrat.DateDebut ||
                sinistre.DateSinistre > contrat.DateFin)
            {
                throw new InvalidDataException(
                    $"Le sinistre {sinistre.ClaimId} est hors de la période contractuelle.");
            }

            if (sinistre.MontantEstime < 0 || sinistre.MontantIndemnisation < 0)
            {
                throw new InvalidDataException(
                    $"Le sinistre {sinistre.ClaimId} contient un montant négatif.");
            }
        }
    }

    private static void EnsureUnique(IEnumerable<string> values, string fieldName)
    {
        var duplicate = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidDataException(
                $"Valeur dupliquée dans {fieldName} : {duplicate.Key}");
        }
    }

    private static List<Dictionary<string, string>> ReadCsv(
        string path,
        IReadOnlyList<string> expectedHeaders)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Fichier d'import introuvable.", path);
        }

        using var parser = new TextFieldParser(path, Encoding.UTF8)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        var headers = parser.ReadFields()
            ?? throw new InvalidDataException($"Le fichier {path} est vide.");
        if (headers.Length > 0)
        {
            headers[0] = headers[0].TrimStart('\uFEFF');
        }

        if (!headers.SequenceEqual(expectedHeaders, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"En-têtes invalides dans {path}. Attendu : {string.Join(", ", expectedHeaders)}");
        }

        var rows = new List<Dictionary<string, string>>();
        while (!parser.EndOfData)
        {
            string[]? fields;
            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException exception)
            {
                throw new InvalidDataException(
                    $"CSV invalide dans {path}, ligne {parser.ErrorLineNumber}.", exception);
            }

            if (fields is null || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (fields.Length != headers.Length)
            {
                throw new InvalidDataException(
                    $"Nombre de colonnes invalide dans {path}, ligne {parser.LineNumber}.");
            }

            rows.Add(headers
                .Select((header, index) => new { header, value = fields[index] })
                .ToDictionary(item => item.header, item => item.value, StringComparer.Ordinal));
        }

        return rows;
    }

    private static string Required(
        IReadOnlyDictionary<string, string> row,
        string column,
        string path,
        int zeroBasedIndex)
    {
        var value = row[column].Trim();
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidDataException(
                $"Valeur obligatoire absente dans {path}, ligne {zeroBasedIndex + 2}, colonne {column}.");
        }

        return value;
    }

    private static DateOnly ParseDate(
        IReadOnlyDictionary<string, string> row,
        string column,
        string path,
        int zeroBasedIndex)
    {
        var value = Required(row, column, path, zeroBasedIndex);
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            throw new InvalidDataException(
                $"Date invalide dans {path}, ligne {zeroBasedIndex + 2}, colonne {column}.");
        }

        return parsed;
    }

    private static decimal ParseDecimal(
        IReadOnlyDictionary<string, string> row,
        string column,
        string path,
        int zeroBasedIndex)
    {
        var value = Required(row, column, path, zeroBasedIndex);
        if (!decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            throw new InvalidDataException(
                $"Montant invalide dans {path}, ligne {zeroBasedIndex + 2}, colonne {column}.");
        }

        return parsed;
    }
}
