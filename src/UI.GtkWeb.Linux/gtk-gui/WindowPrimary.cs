
// This file has been generated by the GUI designer. Do not modify.

public partial class WindowPrimary
{
	private global::Gtk.VBox pnlMain;

	protected virtual void Build ()
	{
		global::Stetic.Gui.Initialize (this);
		// Widget WindowPrimary
		this.Name = "WindowPrimary";
		this.Title = global::Mono.Unix.Catalog.GetString ("MainWindow");
		this.WindowPosition = ((global::Gtk.WindowPosition)(4));
		this.AllowGrow = false;
		// Container child WindowPrimary.Gtk.Container+ContainerChild
		this.pnlMain = new global::Gtk.VBox ();
		this.pnlMain.Name = "pnlMain";
		this.pnlMain.Spacing = 6;
		this.Add (this.pnlMain);
		if ((this.Child != null)) {
			this.Child.ShowAll ();
		}
		this.DefaultWidth = 400;
		this.DefaultHeight = 300;
		this.Show ();
		this.DeleteEvent += new global::Gtk.DeleteEventHandler (this.OnDeleteEvent);
	}
}
