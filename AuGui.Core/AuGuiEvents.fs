namespace AuGui.Core

type AuGuiEvents() =
  let requestFolderDialogEvent = Event<unit>()
  let requestFolderDialogSuccess = Event<Option<string>>()
  let requestFolderDialogError = Event<exn>()

  let requestPageChange = Event<Page>()

  [<CLIEvent>]
  member _.OnRequestFolderDialog = requestFolderDialogEvent.Publish

  [<CLIEvent>]
  member _.OnRequestFolderDialogError = requestFolderDialogError.Publish

  [<CLIEvent>]
  member _.OnRequestFolderDialogSuccess = requestFolderDialogSuccess.Publish

  [<CLIEvent>]
  member _.OnRequestPageChange = requestPageChange.Publish

  member _.RequestFolderDialog() = requestFolderDialogEvent.Trigger()

  member _.RequestPageChange(page: Page) = requestPageChange.Trigger page

  member _.RequestFolderDialogError(ex: exn) =
    requestFolderDialogError.Trigger ex

  member _.RequestFolderDialogSuccess(path: Option<string>) =
    requestFolderDialogSuccess.Trigger path



[<RequireQualifiedAccess>]
module AuGuiEvents =
  let private cls = lazy (AuGuiEvents())
  let OnRequestFolderDialog = cls.Value.OnRequestFolderDialog
  let OnRequestFolderDialogError = cls.Value.OnRequestFolderDialogError
  let OnRequestFolderDialogSuccess = cls.Value.OnRequestFolderDialogSuccess
  let OnRequestPageChange = cls.Value.OnRequestPageChange

  let RequestFolderDialog () = cls.Value.RequestFolderDialog()
  let RequestFolderDialogError (ex: exn) = cls.Value.RequestFolderDialogError ex

  let RequestPageChange (page: Page) = cls.Value.RequestPageChange page

  let RequestFolderDialogSuccess (path: Option<string>) =
    cls.Value.RequestFolderDialogSuccess path
