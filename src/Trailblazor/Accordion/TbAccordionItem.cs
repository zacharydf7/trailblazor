namespace Trailblazor;

/// <summary>One section of a <see cref="TbAccordion"/>: a heading button that opens and closes its panel. It works on
/// its own too, outside an accordion, as a single section that opens and closes by itself under an <c>h3</c>, or the
/// <see cref="HeadingLevel"/> it is given. An item in another item's panel belongs to an accordion around it in that
/// panel, or to none, so opening it never closes the item it sits in.</summary>
public partial class TbAccordionItem;
