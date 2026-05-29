# UpdateCheck

Check any GitHub project for update.

# Usage

Create a `Checker` object with the name of the owner and project you want to check.

````csharp
Checker checker = new Checker("dlebansais", "UpdateCheck");
````

Every time you want to check for a new update, subscribe to the `UpdateStatusChanged` event, then call `CheckUpdate`.

````csharp
checker.UpdateStatusChanged += OnUpdateStatusChanged;
checker.CheckUpdate();
````

When the operation is completed, the result is in `IsUpdateAvailable`.
````csharp
void OnUpdateStatusChanged(object sender, EventArgs e)
{
	Checker checker = (Checker)sender; 
	checker.UpdateStatusChanged -= OnUpdateStatusChanged;

	// Read checker.IsUpdateAvailable
}
````
