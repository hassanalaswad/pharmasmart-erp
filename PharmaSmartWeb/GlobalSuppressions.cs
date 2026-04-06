// This file is used to suppress nullable warnings from CS8618 (non-nullable properties not initialized in constructor)
// and related warnings CS8600, CS8601, CS8602, CS8604, CS8625, CS8629.
// These warnings arise from upgrading a legacy .NET 3.1 project to .NET 8 with Nullable enabled.
// All models in this project originated before Nullable was a mainstream feature.
// They are suppressed globally here and should be resolved gradually per file.

// Suppress CS8618 (Non-nullable property uninitialized) — affects all Model and ViewModel class constructors
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Nullable", "CS8618",
    Justification = "Legacy models upgraded from .NET 3.1. Properties are initialized via EF Core or model binding.")]

// Suppress CS8600 (Converting possible null to non-nullable type) — affects Controllers and Services
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Nullable", "CS8600",
    Justification = "Legacy code from .NET 3.1 upgrade. Null checks are handled by application logic.")]

// Suppress CS8601 (Possible null reference assignment)
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Nullable", "CS8601",
    Justification = "Legacy code from .NET 3.1 upgrade.")]

// Suppress CS8602 (Dereference of possibly null reference) — affects Controllers
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Nullable", "CS8602",
    Justification = "Legacy code from .NET 3.1 upgrade. Navigation properties are ensured via .Include() EF Core calls.")]

// Suppress CS8604 (Possible null reference argument)
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Nullable", "CS8604",
    Justification = "Legacy code from .NET 3.1 upgrade.")]

// Suppress CS8625 (Cannot convert null literal to non-nullable reference type)
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Nullable", "CS8625",
    Justification = "Legacy code from .NET 3.1 upgrade.")]

// Suppress CS8629 (Nullable value type may be null)
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Nullable", "CS8629",
    Justification = "Legacy code from .NET 3.1 upgrade.")]
