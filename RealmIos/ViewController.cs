using System;
using Realms;
using RealmSync.Model;
using UIKit;
using Foundation;

namespace RealmIos
{

	public class TableSource : UITableViewSource
	{

		IRealmCollection<ChatMessage> TableItems;
		string CellIdentifier = "TableCell";

		public TableSource(UITableView table, IRealmCollection<ChatMessage> items)
		{
			TableItems = items;
			items.CollectionChanged += (sender, e) =>
			{
				table.ReloadData(); 
			};

		}

		public override nint RowsInSection(UITableView tableview, nint section)
		{
			return TableItems.Count;
		}

		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
		{
			UITableViewCell cell = tableView.DequeueReusableCell(CellIdentifier);
			var item = TableItems[indexPath.Row];

			//---- if there are no cells to reuse, create a new one
			if (cell == null)
			{ cell = new UITableViewCell(UITableViewCellStyle.Subtitle, CellIdentifier); }

			cell.TextLabel.Text = item.Text;
			cell.DetailTextLabel.Text = item.Author;

			return cell;
		}
	}


	public partial class ViewController : UIViewController
	{
		protected ViewController(IntPtr handle) : base(handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.

			var realm = AppDelegate.Realm;

			Button2.TouchUpInside += (sender, e) =>
			{
				realm.Write(() =>
				{
					realm.Add(new ChatMessage()
					{
						Author = TextField2.Text,
						Text = TextField1.Text,
						DateTime = DateTimeOffset.UtcNow,
						Id = Guid.NewGuid().ToString(),
					});
				});
			};

			TableView.Source = new TableSource(TableView, realm.All<ChatMessage>().AsRealmCollection());
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}
