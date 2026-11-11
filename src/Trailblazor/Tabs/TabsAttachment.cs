using Microsoft.JSInterop;

namespace Trailblazor;

// The tabs' script hands its handle back inside an object, so bUnit's loose mode, which can't build one, hands back
// null and the tabs know that no order report will come.
internal sealed record TabsAttachment(IJSObjectReference Handle);
