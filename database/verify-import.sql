USE AstreeClaimsDb;
GO

SET NOCOUNT ON;

SELECT 'Clients' AS TableName, COUNT(*) AS TotalRows FROM Clients
UNION ALL
SELECT 'Contrats', COUNT(*) FROM Contrats
UNION ALL
SELECT 'Vehicules', COUNT(*) FROM Vehicules
UNION ALL
SELECT 'Sinistres', COUNT(*) FROM Sinistres;
GO

SELECT
    (SELECT COUNT(*)
     FROM Contrats c
     LEFT JOIN Clients cl ON cl.ClientId = c.ClientId
     WHERE cl.ClientId IS NULL) AS ContractsWithoutClient,

    (SELECT COUNT(*)
     FROM Vehicules v
     LEFT JOIN Contrats c ON c.ContractId = v.ContractId
     WHERE c.ContractId IS NULL) AS VehiclesWithoutContract,

    (SELECT COUNT(*)
     FROM Sinistres s
     LEFT JOIN Clients cl ON cl.ClientId = s.ClientId
     WHERE cl.ClientId IS NULL) AS ClaimsWithoutClient,

    (SELECT COUNT(*)
     FROM Sinistres s
     LEFT JOIN Contrats c ON c.ContractId = s.ContractId
     WHERE c.ContractId IS NULL) AS ClaimsWithoutContract,

    (SELECT COUNT(*)
     FROM Sinistres s
     LEFT JOIN Vehicules v ON v.VehicleId = s.VehicleId
     WHERE v.VehicleId IS NULL) AS ClaimsWithoutVehicle,

    (SELECT COUNT(*)
     FROM Sinistres s
     INNER JOIN Contrats c ON c.ContractId = s.ContractId
     WHERE s.ClientId <> c.ClientId) AS ClaimContractClientMismatches,

    (SELECT COUNT(*)
     FROM Sinistres s
     INNER JOIN Vehicules v ON v.VehicleId = s.VehicleId
     WHERE s.ContractId <> v.ContractId) AS ClaimVehicleContractMismatches,

    (SELECT COUNT(*)
     FROM Sinistres s
     INNER JOIN Contrats c ON c.ContractId = s.ContractId
     WHERE s.DateSinistre < c.DateDebut OR s.DateSinistre > c.DateFin)
     AS ClaimsOutsideContractPeriod;
GO

SELECT ClientId, COUNT(*) AS DuplicateCount
FROM Clients
GROUP BY ClientId
HAVING COUNT(*) > 1;

SELECT ContractId, COUNT(*) AS DuplicateCount
FROM Contrats
GROUP BY ContractId
HAVING COUNT(*) > 1;

SELECT VehicleId, COUNT(*) AS DuplicateCount
FROM Vehicules
GROUP BY VehicleId
HAVING COUNT(*) > 1;

SELECT ClaimId, COUNT(*) AS DuplicateCount
FROM Sinistres
GROUP BY ClaimId
HAVING COUNT(*) > 1;
GO

SELECT Statut, COUNT(*) AS ClaimCount
FROM Sinistres
GROUP BY Statut
ORDER BY ClaimCount DESC;
GO

SELECT TypeSinistre, COUNT(*) AS ClaimCount
FROM Sinistres
GROUP BY TypeSinistre
ORDER BY ClaimCount DESC;
GO
