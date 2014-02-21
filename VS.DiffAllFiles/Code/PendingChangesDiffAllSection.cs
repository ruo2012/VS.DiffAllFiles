﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DansKingdom.VS_DiffAllFiles.Code.Base;
using DansKingdom.VS_DiffAllFiles.Code;
using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Controls.Extensibility;

namespace DansKingdom.VS_DiffAllFiles.Code
{
	/// <summary>
	/// Selected file info section.
	/// </summary>
	[TeamExplorerSection(PendingChangesDiffAllSection.SectionId, TeamExplorerPageIds.PendingChanges, 35)]
	public class PendingChangesDiffAllSection : TeamExplorerBaseSection
	{
		#region Members

		public const string SectionId = "D7792573-517F-4B52-898C-CA28E7BDE37E";

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public PendingChangesDiffAllSection()
			: base()
		{
			this.Title = "Diff All Files";
			this.IsExpanded = true;
			this.IsBusy = false;
			this.SectionContent = new PendingChangesDiffAllControl();
			this.View.ParentSection = this;
		}

		/// <summary>
		/// Get the view.
		/// </summary>
		protected PendingChangesDiffAllControl View
		{
			get { return this.SectionContent as PendingChangesDiffAllControl; }
		}

		/// <summary>
		/// Initialize override.
		/// </summary>
		public override void Initialize(object sender, SectionInitializeEventArgs e)
		{
			base.Initialize(sender, e);

			// Find the Pending Changes extensibility service and sign up for
			// property change notifications
			IPendingChangesExt pcExt = this.GetService<IPendingChangesExt>();
			if (pcExt != null)
			{
				pcExt.PropertyChanged += pcExt_PropertyChanged;
			}
		}

		/// <summary>
		/// Dispose override.
		/// </summary>
		public override void Dispose()
		{
			IPendingChangesExt pcExt = this.GetService<IPendingChangesExt>();
			if (pcExt != null)
			{
				pcExt.PropertyChanged -= pcExt_PropertyChanged;
			}

			base.Dispose();
		}

		/// <summary>
		/// Pending Changes Extensibility PropertyChanged event handler.
		/// </summary>
		private void pcExt_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "SelectedIncludedItems":
					Refresh();
					break;
			}
		}

		/// <summary>
		/// Refresh override.
		/// </summary>
		public async override void Refresh()
		{
			base.Refresh();
			//await RefreshAsync();
		}

		public async Task CompareIncludedPendingChanges()
		{
			// Set the Busy flag while we work.
			this.IsBusy = true;

			DiffAllFilesSettings.Settings.CompareOneAtATime = true;

			// Get a handle to the Automation Model that we can use to interact with the VS IDE.
			var dte2 = PackageHelper.DTE2;
			if (dte2 == null)
			{
				ShowNotification("Could not get a handle to the DTE2 (the Visual Studio IDE Automation Model).", NotificationType.Error);
				return;
			}

			//// Make sure we have a connection to Team Foundation.
			//ITeamFoundationContext context = this.CurrentContext;
			//if (context == null || !context.HasCollection) return;
			
			//// Make sure we can access the Version Control Server.
			//VersionControlServer versionControlServer = null;
			//await Task.Run(() => versionControlServer = context.TeamProjectCollection.GetService<VersionControlServer>());
			//if (versionControlServer == null) return;

			// Use the TFS Configured Diff tool for this version of Visual Studio.
			var tfFilePath = Path.Combine(Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName), "TF.exe");
			if (!File.Exists(tfFilePath))
			{
				ShowNotification(string.Format("Could not locate TF.exe. Expected to find it at '{0}'.", tfFilePath), NotificationType.Error);
				return;
			}

			// Save the list of pending changes before looping through them.
			// Only grab the files that are not on the list of file extensions to ignore.
			var pendingChanges = GetService<IPendingChangesExt>();
			if (pendingChanges == null) return;
			var includedItemsList = pendingChanges.IncludedChanges.Where(p => !DiffAllFilesSettings.Settings.FileExtensionsToIgnore.Contains(System.IO.Path.GetExtension(p.LocalOrServerItem))).ToList();
			//var includedItemsList = pendingChanges.IncludedChanges.ToList();

			// Loop through and diff each of the pending changes.
			foreach (var pendingChangeItem in includedItemsList)
			{
				var pendingChange = pendingChangeItem;

				Difference.VisualDiffItems(pendingChange.VersionControlServer, new DiffItemPendingChangeBase(pendingChange),
					new DiffItemLocalFile(pendingChange.LocalItem, pendingChange.Encoding, pendingChange.CreationDate, false), false);

				if (PackageHelper.IsCommandAvailable("Window.KeepTabOpen"))
					dte2.ExecuteCommand("Window.KeepTabOpen");

				//Difference.VisualDiffFiles(pendingChange.VersionControlServer, pendingChange.ServerItem, VersionSpec.Latest, pendingChange.LocalItem, null);

				//var diffProcess = new System.Diagnostics.Process();
				//diffProcess.StartInfo.FileName = tfFilePath;
				//diffProcess.StartInfo.Arguments = string.Format("diff \"{0}\"", pendingChange.LocalOrServerItem);
				//diffProcess.StartInfo.CreateNoWindow = true;
				//diffProcess.StartInfo.UseShellExecute = false;
				//diffProcess.Start();

				//if (DiffAllFilesSettings.CompareOneAtATime)
				//	diffProcess.WaitForExit(10000);
			}

			this.IsBusy = false;
		}

		///// <summary>
		///// Refresh the changeset data asynchronously.
		///// </summary>
		//private async Task RefreshAsync()
		//{
		//	try
		//	{
		//		// Set our busy flag and clear the previous data
		//		this.IsBusy = true;
		//		this.ServerPath = null;
		//		this.LocalPath = null;
		//		this.LatestVersion = null;
		//		this.WorkspaceVersion = null;
		//		this.Encoding = null;

		//		// Temp variables to hold the data as we retrieve it
		//		string serverPath = null, localPath = null;
		//		string latestVersion = null, workspaceVersion = null;
		//		string encoding = null;

		//		// Grab the selected included item from the Pending Changes extensibility object
		//		PendingChangesItem selectedItem = null;
		//		IPendingChangesExt pendingChanges = GetService<IPendingChangesExt>();
		//		if (pendingChanges != null && pendingChanges.SelectedIncludedItems.Length > 0)
		//		{
		//			selectedItem = pendingChanges.SelectedIncludedItems[0];
		//		}
				
		//		if (selectedItem != null && selectedItem.IsPendingChange && selectedItem.PendingChange != null)
		//		{
		//			// Check for rename
		//			if (selectedItem.PendingChange.IsRename && selectedItem.PendingChange.SourceServerItem != null)
		//			{
		//				serverPath = selectedItem.PendingChange.SourceServerItem;
		//			}
		//			else
		//			{
		//				serverPath = selectedItem.PendingChange.ServerItem;
		//			}

		//			localPath = selectedItem.ItemPath;
		//			workspaceVersion = selectedItem.PendingChange.Version.ToString();
		//			encoding = selectedItem.PendingChange.EncodingName;
		//		}
		//		else
		//		{
		//			serverPath = String.Empty;
		//			localPath = selectedItem != null ? selectedItem.ItemPath : String.Empty;
		//			latestVersion = String.Empty;
		//			workspaceVersion = String.Empty;
		//			encoding = String.Empty;
		//		}

		//		// Go get any missing data from the server
		//		if (latestVersion == null || encoding == null)
		//		{
		//			// Make the server call asynchronously to avoid blocking the UI
		//			await Task.Run(() =>
		//			{
		//				ITeamFoundationContext context = this.CurrentContext;
		//				if (context != null && context.HasCollection)
		//				{
		//					VersionControlServer vcs = context.TeamProjectCollection.GetService<VersionControlServer>();
		//					if (vcs != null)
		//					{
		//						Item item = vcs.GetItem(serverPath);
		//						if (item != null)
		//						{
		//							latestVersion = latestVersion ?? item.ChangesetId.ToString();
		//							encoding = encoding ?? FileType.GetEncodingName(item.Encoding);
		//						}
		//					}
		//				}
		//			});
		//		}

		//		// Now back on the UI thread, update the view data
		//		this.ServerPath = serverPath;
		//		this.LocalPath = localPath;
		//		this.LatestVersion = latestVersion;
		//		this.WorkspaceVersion = workspaceVersion;
		//		this.Encoding = encoding;
		//	}
		//	catch (Exception ex)
		//	{
		//		ShowNotification(ex.Message, NotificationType.Error);
		//	}
		//	finally
		//	{
		//		// Always clear our busy flag when done
		//		this.IsBusy = false;
		//	}
		//}

		///// <summary>
		///// Get/set the server path.
		///// </summary>
		//public string ServerPath
		//{
		//	get { return m_serverPath; }
		//	set { m_serverPath = value; RaisePropertyChanged("ServerPath"); }
		//}
		//private string m_serverPath = String.Empty;

		///// <summary>
		///// Get/set the local path.
		///// </summary>
		//public string LocalPath
		//{
		//	get { return m_localPath; }
		//	set { m_localPath = value; RaisePropertyChanged("LocalPath"); }
		//}
		//private string m_localPath = String.Empty;

		///// <summary>
		///// Get/set the latest version.
		///// </summary>
		//public string LatestVersion
		//{
		//	get { return m_latestVersion; }
		//	set { m_latestVersion = value; RaisePropertyChanged("LatestVersion"); }
		//}
		//private string m_latestVersion = String.Empty;

		///// <summary>
		///// Get/set the workspace version.
		///// </summary>
		//public string WorkspaceVersion
		//{
		//	get { return m_workspaceVersion; }
		//	set { m_workspaceVersion = value; RaisePropertyChanged("WorkspaceVersion"); }
		//}
		//private string m_workspaceVersion = String.Empty;

		///// <summary>
		///// Get/set the encoding.
		///// </summary>
		//public string Encoding
		//{
		//	get { return m_encoding; }
		//	set { m_encoding = value; RaisePropertyChanged("Encoding"); }
		//}
		//private string m_encoding = String.Empty;
	}
}