# Skeleton

Pattern: none; it's a placeholder, which can also cover content while it loads

Familiar apps: the gray shapes on a video site's or a social app's feed while it loads

## Keyboard

- [ ] Tab can't reach controls in wrapped content while it loads, even in an open accordion inside it. #333
- [x] Tab reaches them once loading ends. `SkeletonTests.Skeleton_ShouldHideAndCoverItsContentUntilLoaded_WhenWrappingIt`

## Pointer

- [ ] Wrapped content takes no clicks while it loads, even in an open accordion inside it. #333

## Touch

Nothing of its own. A skeleton has nothing to tap.

## Focus

- [x] A plain skeleton takes no focus, and screen readers skip it. `TbSkeletonTests.Render_ShouldBeAHiddenLineOfText_WhenNoParametersAreSet`
- [x] A wrapper is marked busy while it loads. `TbSkeletonTests.Render_ShouldCoverTheContentAndMarkItBusy_WhenWrappingContentThatIsLoading`
- [x] The content's components keep their state when loading ends. `TbSkeletonTests.Render_ShouldKeepTheContentsComponents_WhenLoadingEnds`

## Not applicable

- Moving focus to the content when loading ends: focus stays where the user left it.
