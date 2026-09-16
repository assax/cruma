using Cruma.Notes;
using Cruma.Ui.Services;

namespace Cruma.Ui.Components;

/// <summary>Srozumitelné české zprávy pro chyby datových služeb (error-handling-policy.md P-5).</summary>
public static class ErrorText
{
    public static string For(DataError error) => (error.Rule ?? error.Code) switch
    {
        DataError.Offline => "Bez připojení to nejde. Zkuste to po obnovení spojení.",
        NotesErrorCodes.DefaultCategoryCannotBeDeleted => "Výchozí kategorii nejde smazat.",
        NotesErrorCodes.NoteNotInTrash => "Trvale smazat jde jen poznámku v koši.",
        NotesErrorCodes.NameRequired => "Název nesmí být prázdný.",
        NotesErrorCodes.InvalidStateTransition => "Tuto změnu stavu nejde provést.",
        "not_found" => "Položka už neexistuje.",
        "unauthenticated" => "Přihlášení vypršelo, přihlaste se znovu.",
        "client_version_unsupported" => "Aplikace je zastaralá, načtěte ji znovu.",
        _ => string.IsNullOrWhiteSpace(error.Message) ? "Něco se nepovedlo." : error.Message,
    };
}
