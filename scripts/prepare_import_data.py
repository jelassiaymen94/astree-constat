from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any

import pandas as pd

REQUIRED_SHEETS = {
    "Clients": ["Client_ID", "Nom", "Prénom", "Gouvernorat"],
    "Polices_Assurance": [
        "Contract_ID",
        "Client_ID",
        "Date_Debut_Contrat",
        "Date_Fin_Contrat",
        "Type_Couverture",
    ],
    "Vehicules": [
        "Vehicle_ID",
        "Contract_ID",
        "Type_Vehicule",
        "Marque",
        "Modele",
        "Immatriculation",
    ],
    "Sinistres": [
        "Claim_ID",
        "Contract_ID",
        "Client_ID",
        "Vehicle_ID",
        "Date_Sinistre_Claim",
        "Type_Sinistre_Claim",
        "Description_Sinistre_Claim",
        "Montant_Estime_Dommage_Claim",
        "Montant_Indemnisation_Claim",
        "Statut_Sinistre_Claim",
    ],
}

SQL_LIMITS = {
    "Clients": {"ClientId": 20, "Nom": 100, "Prenom": 100, "Gouvernorat": 100},
    "Contrats": {"ContractId": 20, "ClientId": 20, "TypeCouverture": 100},
    "Vehicules": {
        "VehicleId": 20,
        "ContractId": 20,
        "TypeVehicule": 50,
        "Marque": 50,
        "Modele": 100,
        "Immatriculation": 30,
    },
    "Sinistres": {
        "ClaimId": 20,
        "ContractId": 20,
        "ClientId": 20,
        "VehicleId": 20,
        "TypeSinistre": 100,
        "Statut": 50,
    },
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Prépare les fichiers CSV validés pour AstreeClaimsDb."
    )
    parser.add_argument("--input", required=True, type=Path, help="Fichier Excel source")
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=Path("data/processed"),
        help="Dossier des fichiers générés",
    )
    return parser.parse_args()


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def require_columns(sheets: dict[str, pd.DataFrame]) -> None:
    missing_sheets = sorted(set(REQUIRED_SHEETS) - set(sheets))
    if missing_sheets:
        raise ValueError(f"Feuilles manquantes : {', '.join(missing_sheets)}")

    for sheet_name, required_columns in REQUIRED_SHEETS.items():
        missing_columns = sorted(set(required_columns) - set(sheets[sheet_name].columns))
        if missing_columns:
            raise ValueError(
                f"Colonnes manquantes dans {sheet_name} : {', '.join(missing_columns)}"
            )


def normalize_text(frame: pd.DataFrame, columns: list[str]) -> None:
    for column in columns:
        frame[column] = frame[column].astype("string").str.strip()


def assert_no_nulls(frame: pd.DataFrame, table: str) -> None:
    null_counts = frame.isna().sum()
    failures = {column: int(count) for column, count in null_counts.items() if count > 0}
    if failures:
        raise ValueError(f"Valeurs obligatoires absentes dans {table} : {failures}")


def assert_unique(frame: pd.DataFrame, column: str, table: str) -> None:
    duplicate_count = int(frame[column].duplicated().sum())
    if duplicate_count:
        raise ValueError(f"{table}.{column} contient {duplicate_count} doublon(s)")


def assert_lengths(frame: pd.DataFrame, table: str) -> dict[str, dict[str, Any]]:
    checks: dict[str, dict[str, Any]] = {}
    for column, limit in SQL_LIMITS[table].items():
        maximum = int(frame[column].astype("string").str.len().max()) if len(frame) else 0
        checks[column] = {"maximum": maximum, "sqlLimit": limit, "valid": maximum <= limit}
        if maximum > limit:
            raise ValueError(
                f"{table}.{column} dépasse NVARCHAR({limit}) : longueur maximale {maximum}"
            )
    return checks


def write_csv(frame: pd.DataFrame, path: Path) -> None:
    frame.to_csv(path, index=False, encoding="utf-8-sig", lineterminator="\n")


def main() -> None:
    args = parse_args()
    input_path = args.input.resolve()
    output_dir = args.output_dir.resolve()

    if not input_path.is_file():
        raise FileNotFoundError(f"Fichier source introuvable : {input_path}")

    sheets = pd.read_excel(input_path, sheet_name=None, dtype=object)
    require_columns(sheets)

    clients_raw = sheets["Clients"][REQUIRED_SHEETS["Clients"]].copy()
    contracts_raw = sheets["Polices_Assurance"][REQUIRED_SHEETS["Polices_Assurance"]].copy()
    vehicles_raw = sheets["Vehicules"][REQUIRED_SHEETS["Vehicules"]].copy()
    claims_raw = sheets["Sinistres"][REQUIRED_SHEETS["Sinistres"]].copy()

    normalize_text(clients_raw, ["Client_ID", "Nom", "Prénom", "Gouvernorat"])
    normalize_text(
        contracts_raw, ["Contract_ID", "Client_ID", "Type_Couverture"]
    )
    normalize_text(
        vehicles_raw,
        [
            "Vehicle_ID",
            "Contract_ID",
            "Type_Vehicule",
            "Marque",
            "Modele",
            "Immatriculation",
        ],
    )
    normalize_text(
        claims_raw,
        [
            "Claim_ID",
            "Contract_ID",
            "Client_ID",
            "Vehicle_ID",
            "Type_Sinistre_Claim",
            "Description_Sinistre_Claim",
            "Statut_Sinistre_Claim",
        ],
    )

    contracts_raw["Date_Debut_Contrat"] = pd.to_datetime(
        contracts_raw["Date_Debut_Contrat"], errors="coerce"
    ).dt.normalize()
    contracts_raw["Date_Fin_Contrat"] = pd.to_datetime(
        contracts_raw["Date_Fin_Contrat"], errors="coerce"
    ).dt.normalize()
    claims_raw["Date_Sinistre_Claim"] = pd.to_datetime(
        claims_raw["Date_Sinistre_Claim"], errors="coerce"
    ).dt.normalize()
    claims_raw["Montant_Estime_Dommage_Claim"] = pd.to_numeric(
        claims_raw["Montant_Estime_Dommage_Claim"], errors="coerce"
    )
    claims_raw["Montant_Indemnisation_Claim"] = pd.to_numeric(
        claims_raw["Montant_Indemnisation_Claim"], errors="coerce"
    )

    assert_no_nulls(clients_raw, "Clients source")
    assert_no_nulls(contracts_raw, "Contrats source")
    assert_no_nulls(vehicles_raw, "Véhicules source")
    assert_no_nulls(claims_raw, "Sinistres source")

    assert_unique(clients_raw, "Client_ID", "Clients source")
    assert_unique(contracts_raw, "Contract_ID", "Contrats source")
    assert_unique(vehicles_raw, "Vehicle_ID", "Véhicules source")
    assert_unique(vehicles_raw, "Contract_ID", "Véhicules source")
    assert_unique(claims_raw, "Claim_ID", "Sinistres source")

    invalid_contract_periods = contracts_raw[
        contracts_raw["Date_Fin_Contrat"] < contracts_raw["Date_Debut_Contrat"]
    ]
    if not invalid_contract_periods.empty:
        raise ValueError(
            f"{len(invalid_contract_periods)} contrat(s) ont une période invalide"
        )

    if (claims_raw["Montant_Estime_Dommage_Claim"] < 0).any():
        raise ValueError("Un ou plusieurs montants estimés sont négatifs")
    if (claims_raw["Montant_Indemnisation_Claim"] < 0).any():
        raise ValueError("Une ou plusieurs indemnisations sont négatives")

    joined = claims_raw.merge(
        contracts_raw,
        on="Contract_ID",
        how="left",
        suffixes=("_Claim", "_Contract"),
        validate="many_to_one",
        indicator="ContractJoin",
    )
    joined = joined.merge(
        vehicles_raw[["Vehicle_ID", "Contract_ID"]].rename(
            columns={"Contract_ID": "VehicleContract_ID"}
        ),
        on="Vehicle_ID",
        how="left",
        validate="many_to_one",
        indicator="VehicleJoin",
    )

    client_ids = set(clients_raw["Client_ID"])
    relation_failures = {
        "missingContract": int(joined["ContractJoin"].ne("both").sum()),
        "missingVehicle": int(joined["VehicleJoin"].ne("both").sum()),
        "missingClaimClient": int((~joined["Client_ID_Claim"].isin(client_ids)).sum()),
        "missingContractClient": int(
            (~joined["Client_ID_Contract"].isin(client_ids)).sum()
        ),
        "claimContractClientMismatch": int(
            joined["Client_ID_Claim"].ne(joined["Client_ID_Contract"]).sum()
        ),
        "vehicleContractMismatch": int(
            joined["VehicleContract_ID"].ne(joined["Contract_ID"]).sum()
        ),
    }
    if any(relation_failures.values()):
        raise ValueError(f"Relations incohérentes : {relation_failures}")

    before_start = joined["Date_Sinistre_Claim"] < joined["Date_Debut_Contrat"]
    after_end = joined["Date_Sinistre_Claim"] > joined["Date_Fin_Contrat"]
    temporal_valid = ~(before_start | after_end)

    valid_joined = joined[temporal_valid].copy()
    excluded_joined = joined[~temporal_valid].copy()
    excluded_joined["Reason"] = ""
    excluded_joined.loc[before_start[~temporal_valid], "Reason"] = "BEFORE_CONTRACT_START"
    excluded_joined.loc[after_end[~temporal_valid], "Reason"] = "AFTER_CONTRACT_END"

    selected_contract_ids = set(valid_joined["Contract_ID"])
    selected_vehicle_ids = set(valid_joined["Vehicle_ID"])
    selected_client_ids = set(valid_joined["Client_ID_Claim"])

    clients = (
        clients_raw[clients_raw["Client_ID"].isin(selected_client_ids)]
        .rename(columns={"Client_ID": "ClientId", "Prénom": "Prenom"})
        [["ClientId", "Nom", "Prenom", "Gouvernorat"]]
        .sort_values("ClientId")
        .reset_index(drop=True)
    )
    contracts = (
        contracts_raw[contracts_raw["Contract_ID"].isin(selected_contract_ids)]
        .rename(
            columns={
                "Contract_ID": "ContractId",
                "Client_ID": "ClientId",
                "Type_Couverture": "TypeCouverture",
                "Date_Debut_Contrat": "DateDebut",
                "Date_Fin_Contrat": "DateFin",
            }
        )
        [["ContractId", "ClientId", "TypeCouverture", "DateDebut", "DateFin"]]
        .sort_values("ContractId")
        .reset_index(drop=True)
    )
    vehicles = (
        vehicles_raw[vehicles_raw["Vehicle_ID"].isin(selected_vehicle_ids)]
        .rename(
            columns={
                "Vehicle_ID": "VehicleId",
                "Contract_ID": "ContractId",
                "Type_Vehicule": "TypeVehicule",
            }
        )
        [["VehicleId", "ContractId", "TypeVehicule", "Marque", "Modele", "Immatriculation"]]
        .sort_values("VehicleId")
        .reset_index(drop=True)
    )
    claims = (
        valid_joined.rename(
            columns={
                "Claim_ID": "ClaimId",
                "Contract_ID": "ContractId",
                "Client_ID_Claim": "ClientId",
                "Vehicle_ID": "VehicleId",
                "Date_Sinistre_Claim": "DateSinistre",
                "Type_Sinistre_Claim": "TypeSinistre",
                "Description_Sinistre_Claim": "Description",
                "Montant_Estime_Dommage_Claim": "MontantEstime",
                "Montant_Indemnisation_Claim": "MontantIndemnisation",
                "Statut_Sinistre_Claim": "Statut",
            }
        )[
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
                "Statut",
            ]
        ]
        .sort_values("ClaimId")
        .reset_index(drop=True)
    )

    for frame, date_columns in [
        (contracts, ["DateDebut", "DateFin"]),
        (claims, ["DateSinistre"]),
    ]:
        for column in date_columns:
            frame[column] = frame[column].dt.strftime("%Y-%m-%d")

    for column in ["MontantEstime", "MontantIndemnisation"]:
        claims[column] = claims[column].map(lambda value: f"{float(value):.2f}")

    expected_counts = {
        "Clients": 2048,
        "Contrats": 2179,
        "Vehicules": 2179,
        "Sinistres": 3460,
    }
    actual_counts = {
        "Clients": len(clients),
        "Contrats": len(contracts),
        "Vehicules": len(vehicles),
        "Sinistres": len(claims),
    }
    if actual_counts != expected_counts:
        raise ValueError(
            f"Comptages inattendus. Attendu={expected_counts}, obtenu={actual_counts}"
        )

    assert_unique(clients, "ClientId", "Clients")
    assert_unique(contracts, "ContractId", "Contrats")
    assert_unique(vehicles, "VehicleId", "Vehicules")
    assert_unique(vehicles, "ContractId", "Vehicules")
    assert_unique(claims, "ClaimId", "Sinistres")

    final_relations = {
        "contractsWithoutClient": int((~contracts["ClientId"].isin(clients["ClientId"])).sum()),
        "vehiclesWithoutContract": int(
            (~vehicles["ContractId"].isin(contracts["ContractId"])).sum()
        ),
        "claimsWithoutClient": int((~claims["ClientId"].isin(clients["ClientId"])).sum()),
        "claimsWithoutContract": int(
            (~claims["ContractId"].isin(contracts["ContractId"])).sum()
        ),
        "claimsWithoutVehicle": int(
            (~claims["VehicleId"].isin(vehicles["VehicleId"])).sum()
        ),
    }
    if any(final_relations.values()):
        raise ValueError(f"Relations finales incohérentes : {final_relations}")

    length_checks = {
        "Clients": assert_lengths(clients, "Clients"),
        "Contrats": assert_lengths(contracts, "Contrats"),
        "Vehicules": assert_lengths(vehicles, "Vehicules"),
        "Sinistres": assert_lengths(claims, "Sinistres"),
    }

    output_dir.mkdir(parents=True, exist_ok=True)
    write_csv(clients, output_dir / "clients.csv")
    write_csv(contracts, output_dir / "contrats.csv")
    write_csv(vehicles, output_dir / "vehicules.csv")
    write_csv(claims, output_dir / "sinistres.csv")
    write_csv(
        excluded_joined[["Claim_ID", "Contract_ID", "Date_Sinistre_Claim", "Reason"]]
        .rename(
            columns={
                "Claim_ID": "ClaimId",
                "Contract_ID": "ContractId",
                "Date_Sinistre_Claim": "DateSinistre",
            }
        )
        .sort_values("ClaimId"),
        output_dir / "sinistres_exclus.csv",
    )

    report = {
        "sourceFile": input_path.name,
        "sourceSha256": sha256(input_path),
        "rule": "Date_Debut_Contrat <= Date_Sinistre_Claim <= Date_Fin_Contrat",
        "sourceRows": {name: int(len(sheets[name])) for name in REQUIRED_SHEETS},
        "claims": {
            "analyzed": int(len(claims_raw)),
            "valid": int(len(claims)),
            "excluded": int(len(excluded_joined)),
            "excludedBeforeContractStart": int(before_start.sum()),
            "excludedAfterContractEnd": int(after_end.sum()),
        },
        "generatedRows": actual_counts,
        "relationChecks": final_relations,
        "sourceRelationChecks": relation_failures,
        "lengthChecks": length_checks,
        "statusCounts": {
            str(key): int(value) for key, value in claims["Statut"].value_counts().items()
        },
        "claimTypeCounts": {
            str(key): int(value)
            for key, value in claims["TypeSinistre"].value_counts().items()
        },
        "outputFiles": [
            "clients.csv",
            "contrats.csv",
            "vehicules.csv",
            "sinistres.csv",
            "sinistres_exclus.csv",
        ],
    }
    with (output_dir / "import-report.json").open("w", encoding="utf-8") as destination:
        json.dump(report, destination, ensure_ascii=False, indent=2)

    print("Préparation terminée avec succès")
    print(f"Source SHA-256 : {report['sourceSha256']}")
    print(f"Clients   : {len(clients)}")
    print(f"Contrats  : {len(contracts)}")
    print(f"Vehicules : {len(vehicles)}")
    print(f"Sinistres : {len(claims)}")
    print(f"Exclus    : {len(excluded_joined)}")
    print(f"Sortie    : {output_dir}")


if __name__ == "__main__":
    main()
