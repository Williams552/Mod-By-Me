# Matrilineal Gene - Technical Overview & Architecture

## 1. Introduction

**Matrilineal Gene** (`william.matrilinealgene`) introduces a matriarchal genetics mechanic to RimWorld Biotech.

When a pawn possesses the `Gene_MatrilinealBirth` gene:
1. **Sex Determination:** The child generated from pregnancy or growth vats is strictly female.
2. **Genetics & Xenotype:** The daughter inherits the genetic mother's exact endogene makeup and xenotype classification without standard 50% hybrid dilution.

---

## 2. Technical Architecture

### Harmony Patch Points

| Target Method | Patch Type | Purpose |
| :--- | :---: | :--- |
| `RimWorld.PregnancyUtility.GetInheritedGenes` | **Prefix** | Overrides random 50/50 parental gene crossover with full maternal endogene replication. |
| `RimWorld.PregnancyUtility.TryGetInheritedXenotype` | **Prefix** | Directly returns the mother's XenotypeDef as inherited. |
| `RimWorld.PregnancyUtility.ShouldByHybrid` | **Prefix** | Prevents flagging pure-lineage daughters as hybrids. |
| `RimWorld.PregnancyUtility.ApplyBirthOutcome` | **Prefix & Postfix** | Sets generation context, enforces female gender, copies custom xenotype metadata (name/icon), and emits birth notification. |
| `Verse.PawnGenerator.GenerateNewPawnInternal` | **Prefix** | Forces `request.FixedGender = Gender.Female` when `GeneratingMatrilinealBirth` context is active. |
| `Verse.Hediff_Pregnant.DoBirthSpawn` | **Prefix & Postfix** | Safeguards fallback and animal birth pathways. |

---

## 3. Data Definitions (Defs)

- **`GeneDef`**: `Gene_MatrilinealBirth`
  - Category: `Reproduction`
  - Biostats: `biostatCpx = 1`, `biostatMet = 0`, `biostatArc = 0`
  - Inheritability: `canGenerateInGeneSet = true`
- **`GeneDefs_Matrilineal.xml`**: Defined in `1.5/Defs/` and `1.6/Defs/`.

---

## 4. Multi-Version Support

- Supports RimWorld **1.5** and **1.6** through dual version output directories and `LoadFolders.xml`.
