# Feature spec

- Add and Populate Wiki.SelectedPages
- Using Wiki.GetPagesBySearchStr, powered by Page.ContainsText
- Search has options for case sensitivity
- GUI will select pages using Wiki.SelectedPages
- Async search does not freeze UI

## Planned related features

Features planned next:

- Core will populate Wiki.SelectedPages using Wiki.GetPagesByTags and Wiki.GetPagesByLinks
- Wiki.SelectedPages will be used for bulk operations like Copy, Delete, and AddTagsToSelectedPages
