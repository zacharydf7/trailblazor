namespace Trailblazor;

// Gets each row the user chooses in a single-selection tree, even one already selected, so a tree select can close
// its popup when its current choice is clicked again.
internal interface ITreeChoice
{
    Task ChosenAsync(object key);
}
