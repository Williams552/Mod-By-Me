# 📚 Mandatory Documentation Workflow Directive

## Strict Rule
Whenever ANY code modification, bug fix, feature addition, parameter re-balancing, or refactoring is performed in this codebase:

1. **Mandatory Documentation Review & Update Step:** You MUST immediately review and update all relevant documentation files:
   - `actual_features_report.md` (master technical feature report artifact)
   - Root `README.md` (English & Vietnamese public documentation)
   - `FireDiscipline/README.md` (synced module documentation)
2. **100% Code-to-Docs Alignment:** Ensure zero discrepancy between C# implementation, XML defs, mod options UI, and documentation descriptions.
3. **Commit & Deploy Integrity:** Never perform a release commit or git push without verifying that documentation has been updated to reflect the exact current codebase state.
