using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Net;
using System.Threading;

namespace DfB_Explorer
{
    public partial class FormMain : Form
    {
        DfBTeam gbl_TeamObject;
        int gbl_current_member_index;

        [DataContract]
        class FolderListing
        {
            //folder listing:
            [DataMember]
            public string size { get; set; }
            [DataMember]
            public string hash { get; set; }
            [DataMember]
            public double bytes { get; set; }
            [DataMember]
            public string thumb_exists { get; set; }
            [DataMember]
            public string rev { get; set; }
            [DataMember]
            public string modified { get; set; }
            [DataMember]
            public string path { get; set; }
            [DataMember]
            public string is_dir { get; set; }
            [DataMember]
            public string icon { get; set; }
            [DataMember]
            public string root { get; set; }
            [DataMember]
            public string revision { get; set; }

            //not in sample
            [DataMember]
            public string is_deleted { get; set; }
            [DataMember]
            public string photo_info { get; set; }
            [DataMember]
            public string video_info { get; set; }
            [DataMember]
            public string client_mtime { get; set; }
            [DataMember]
            public List<Shared_Folder> shared_folder { get; set; }
            [DataMember]
            public string read_only { get; set; }
            [DataMember]
            public string parent_shared_folder_id { get; set; }
            [DataMember]
            public string modifier { get; set; }
            [DataMember]
            public List<FolderContent> contents { get; set; }
        }

        [DataContract]
        class Shared_Folder
        {
            //permissions:
            [DataMember]
            public string shared_folder_id { get; set; }
        }

        [DataContract]
        class FolderContent
        {
            [DataMember]
            public string size { get; set; }
            [DataMember]
            public string rev { get; set; }
            [DataMember]
            public double bytes { get; set; }
            [DataMember]
            public string thumb_exists { get; set; }
            [DataMember]
            public string modified { get; set; }
            [DataMember]
            public string client_mtime { get; set; }
            [DataMember]
            public string path { get; set; }
            [DataMember]
            public string photo_info { get; set; }
            [DataMember]
            public string is_dir { get; set; }
            [DataMember]
            public string icon { get; set; }
            [DataMember]
            public string root { get; set; }
            [DataMember]
            public string mime_type { get; set; }
            [DataMember]
            public string revision { get; set; }
        }

        [DataContract]
        class BasicTeamInformation
        {
            //folder listing:
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string team_id { get; set; }
            [DataMember]
            public int num_licenced_users { get; set; }
            [DataMember]
            public int num_provisioned_users { get; set; }
        }

        [DataContract]
        class Profile
        {
            //profile:
            [DataMember]
            public string given_name { get; set; }
            [DataMember]
            public string surname { get; set; }
            [DataMember]
            public string status { get; set; }
            [DataMember]
            public string member_id { get; set; }
            [DataMember]
            public string email { get; set; }
            [DataMember]
            public string external_id { get; set; }
            [DataMember]
            public List<string> groups { get; set; }
        }

        [DataContract]
        class Permissions
        {
            //permissions:
            [DataMember]
            public bool is_admin { get; set; }
        }

        [DataContract]
        class Member
        //member is made up of profile and permissions components
        {
            [DataMember]
            public Profile profile { get; set; }
            [DataMember]
            public Permissions permissions { get; set; }
        }

        [DataContract]
        class DfBTeam
        {
            //team is made up of members and has a cursor and has_more attribute
            [DataMember]
            public List<Member> members { get; set; }
            [DataMember]
            public string cursor { get; set; }
            [DataMember]
            public bool has_more { get; set; }
        }

        public FormMain()
        {
            InitializeComponent();

            //register an event to handle the treeview clicks.
            treeViewUsers.NodeMouseClick +=
                new TreeNodeMouseClickEventHandler(treeViewUsers_NodeMouseClick);

            //changed this to afterselect.
            //treeViewFiles.NodeMouseClick +=
                //new TreeNodeMouseClickEventHandler(treeViewFiles_NodeMouseClick);

            treeViewFiles.AfterSelect +=
                new TreeViewEventHandler(treeViewFiles_AfterSelect);

            treeViewFiles.NodeMouseDoubleClick +=
                new TreeNodeMouseClickEventHandler(treeViewFiles_NodeMouseDoubleClick);

        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            //temporary for testing:
            txtToken.Text = "234token"; //hanfordinc.com

            pictureBoxConnected.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxConnected.Image = imageList2.Images[2];
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            // Set cursor as hourglass
            Cursor.Current = Cursors.WaitCursor;
            
            //obtain basic api information
            BasicTeamInformation teamInformation = dfbBasicTeamInfo();
            //obtain team information
            DfBTeam dfbTeam = dfb_members_list();

            //if we connect to the api successfully:
            if (teamInformation.num_licenced_users >= 0)
            {
                labelTeamName.Text = "Connected to: " + teamInformation.name;
                pictureBoxConnected.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBoxConnected.Image = imageList2.Images[1];
                //populate the global object for use throughout the app.
                gbl_TeamObject = dfbTeam;
                populateDfBMembers(gbl_TeamObject);
            }

            // Set cursor as default arrow
            Cursor.Current = Cursors.Default;    
        }

        public void treeViewFiles_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            Console.WriteLine("Double Click");
            Console.WriteLine("Full path: " + e.Node.FullPath);
            //pop the save file dialog
            saveFileDialog1.FileOk += saveFileDialog1_FileOk;
            saveFileDialog1.FileName = e.Node.Text;
            saveFileDialog1.ShowDialog();

            //we need the path of the file we want to save
            IList<int> path = GetNodePathIndexes(e.Node);

            StringBuilder fullPath = new StringBuilder("treeview");
            foreach (int index in path)
            {
                fullPath.AppendFormat(".Nodes[{0}]", index);
            }
            Console.WriteLine("After showdialog Full path: " + fullPath);

            //implement the save...
            string fixedpath = e.Node.FullPath.Replace("\\", "/");
            Console.WriteLine("fixed path: " + fixedpath);
            Console.WriteLine("from teh dialog: " + saveFileDialog1.FileName);

            Cursor.Current = Cursors.WaitCursor;
            download_file(fixedpath, txtToken.Text, gbl_TeamObject.members[gbl_current_member_index].profile.member_id, saveFileDialog1.FileName);
            Cursor.Current = Cursors.Default;
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            //throw new NotImplementedException();
            Console.WriteLine("we made it");
        }

        public IList<int> GetNodePathIndexes(TreeNode node)
        {
            List<int> indexes = new List<int>();
            TreeNode currentNode = node;
            while (currentNode != null)
            {
                TreeNode parentNode = currentNode.Parent;
                if (parentNode != null)
                    indexes.Add(parentNode.Nodes.IndexOf(currentNode));
                else
                    indexes.Add(currentNode.TreeView.Nodes.IndexOf(currentNode));

                currentNode = parentNode;
            }
            indexes.Reverse();
            return indexes;
        }

        void treeViewUsers_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeViewFiles.Nodes.Clear();
            gbl_current_member_index = e.Node.Index;
            FolderListing rootFolderListing = Get_listing("/", txtToken.Text, gbl_TeamObject.members[gbl_current_member_index].profile.member_id);
            foreach (FolderContent fc in rootFolderListing.contents)
            {
                //strip the backslash for display in the tree
                string tmp = fc.path;
                string tmpWithoutSlash = tmp.Substring(1);

                TreeNode treeNode = new TreeNode(tmpWithoutSlash);

                //change icon for files/folders
                if (fc.is_dir == "false")
                {
                    treeNode.ImageKey = "file";
                    treeNode.SelectedImageKey = "file";
                }
                else
                {
                    treeNode.ImageKey = "folder_closed";
                }
                treeViewFiles.Nodes.Add(treeNode);
            }
        }

        protected void treeViewFiles_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            Console.WriteLine("Node Clicked: " + e.Node.Index);
            if (e.Node.Parent != null)
                Console.WriteLine("Node Clicked Parent: " + e.Node.Parent.Index);
            else
                Console.WriteLine("Node Clicked Parent: no parent");

            //dont re-do the call if the node is already populated
            if (e.Node.IsExpanded == false && e.Node.ImageKey != "file")
            {
                string tmpPath = "/" + e.Node.FullPath;
                string slashPath = tmpPath.Replace("\\", "/");
                FolderListing subfolder = Get_listing(slashPath, txtToken.Text, gbl_TeamObject.members[gbl_current_member_index].profile.member_id);
                if (subfolder.contents != null && subfolder.contents.Count > 0)
                    //this prevents the duplicates showing up when clicking same folder again...
                    RemoveChildNodes(e.Node);
                    foreach (FolderContent fc in subfolder.contents)
                    {
                        //strip the backslash and parent folder deets for display in the tree
                        string tmp = fc.path;
                        int originatingFolderLength = slashPath.Length + 1;
                        string tmpWithoutSlash = tmp.Substring(originatingFolderLength);

                        treeViewFiles.BeginUpdate();
                        TreeNode treeNode = new TreeNode(tmpWithoutSlash);

                        //change icon for files/folders
                        if (fc.is_dir == "false")
                        {
                            treeNode.ImageKey = "file";
                            treeNode.SelectedImageKey = "file";
                        }
                        else
                        {
                            treeNode.ImageKey = "folder_closed";
                        }

                        string fullpath = treeViewFiles.SelectedNode.FullPath.ToString();
                        e.Node.Nodes.Add(treeNode);

                        treeViewFiles.EndUpdate();
                        e.Node.ExpandAll();
                    }
            }
            else
            {
                //second click user wants to collapse tree
                e.Node.Collapse();
            }
        }

        private void RemoveChildNodes(TreeNode aNode)
        {
            if (aNode.Nodes.Count > 0)
            {
                for (int i = aNode.Nodes.Count - 1; i >= 0; i--)
                {
                    aNode.Nodes[i].Remove();
                }
            }

        }

        private void populateDfBMembers(DfBTeam dfbTeam)
        {
            int totalMembers = dfbTeam.members.Count;
            for (int i = 0; i < totalMembers; i++)
            {
                string fullname = dfbTeam.members[i].profile.given_name + " " + dfbTeam.members[i].profile.surname;
                if (fullname != " ")
                {
                    if (dfbTeam.members[i].profile.status == "active")
                    {
                        TreeNode treeNode = new TreeNode(fullname);
                        treeViewUsers.Nodes.Add(treeNode);
                    }
                    else
                    {
                        //lets nots show inactives yet.
                    }
                }
                else
                {
                    fullname = "None Provided";
                    if (dfbTeam.members[i].profile.status == "active")
                    {
                        TreeNode treeNode = new TreeNode(fullname);
                        treeViewUsers.Nodes.Add(treeNode);
                    }
                    else
                    {
                        //lets not show inactives yet.
                    }
                }
            }
        }

        private BasicTeamInformation dfbBasicTeamInfo()
        {
            String page = "https://api.dropbox.com/1/team/get_info";
            WebRequest request = WebRequest.Create(page);

            request.Method = "POST";
            request.ContentType = "application/json";
            String authheader = "Authorization:Bearer " + txtToken.Text;
            request.Headers.Add(authheader);

            // Create POST data and convert it to a byte array.
            string postData = "{}";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            Stream ds = request.GetRequestStream();

            // Write the data to the request stream.
            ds.Write(byteArray, 0, byteArray.Length);

            // Close the Stream object.
            ds.Close();

            // Get the response.
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();

                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);

                // Read the content. 
                string responseFromServer = reader.ReadToEnd();

                // Cleanup the streams and the response.
                reader.Close();
                dataStream.Close();
                response.Close();

                //serialise the json so we can send back a BasicTeamInformation object
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(BasicTeamInformation));
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseFromServer));
                BasicTeamInformation teamObj = (BasicTeamInformation)ser.ReadObject(stream);

                return teamObj;
            }

            catch (WebException error)
            {
                BasicTeamInformation failedObj = new BasicTeamInformation();
                failedObj.name = error.Message;
                failedObj.num_licenced_users = -1;
                failedObj.num_provisioned_users = -1;
                return failedObj;
            }
        }

        private DfBTeam dfb_members_list()
        {
            String page = "https://api.dropbox.com/1/team/members/list";
            WebRequest request = WebRequest.Create(page);

            request.Method = "POST";
            request.ContentType = "application/json";
            String authheader = "Authorization:Bearer " + txtToken.Text;
            request.Headers.Add(authheader);

            // Create POST data and convert it to a byte array.
            string postData = "{}";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            Stream ds = request.GetRequestStream();

            // Write the data to the request stream.
            ds.Write(byteArray, 0, byteArray.Length);

            // Close the Stream object.
            ds.Close();

            // Get the response.
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();

                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);

                // Read the content. 
                string responseFromServer = reader.ReadToEnd();

                // Cleanup the streams and the response.
                reader.Close();
                dataStream.Close();
                response.Close();

                //serialise the json so we can send back a BasicTeamInformation object
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(DfBTeam));
                MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseFromServer));
                DfBTeam teamObj = (DfBTeam)ser.ReadObject(stream);

                return teamObj;
            }

            catch (WebException error)
            {
                DfBTeam failedObj = new DfBTeam();
                failedObj.cursor = error.Message;
                return failedObj;
            }

        }

        private FolderListing Get_listing(string folderpath, string dfb_token, string dfb_member_id)
        {
            
            if (folderpath == null)
            {
                Console.WriteLine("Empty path");
            }

            else
            {
                string uri = "https://api.dropbox.com/1/metadata/auto" + folderpath; // +"?list=false";
                WebRequest request = WebRequest.Create(uri);

                request.Method = "GET";
                request.ContentType = "application/json";
                String authheader = "Authorization:Bearer " + dfb_token;
                request.Headers.Add(authheader);
                request.Headers.Add("X-Dropbox-Perform-As-Team-Member:" + dfb_member_id);

                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    // Get the stream containing content returned by the server.
                    Stream dataStream = response.GetResponseStream();

                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);

                    // Read the content. 
                    string responseFromServer = reader.ReadToEnd();

                    //serialize the json
                    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(FolderListing));
                    MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(responseFromServer));
                    try
                    {
                        var obj = (FolderListing)ser.ReadObject(stream);

                        // Cleanup the streams and the response.
                        reader.Close();
                        dataStream.Close();
                        response.Close();

                        return obj;
                    }
                    catch
                    {
                        FolderListing failListing = new FolderListing();
                        failListing.bytes = -2;
                        return failListing;
                    }

                }

                catch (WebException error)
                {
                    Console.WriteLine(error);
                }

            }
            FolderListing brokenListing = new FolderListing();
            brokenListing.bytes = -1;
            return brokenListing;
        } //end of get_listing

        private void download_file(string folderpath, string dfb_token, string dfb_member_id, string output_file)
        {

            //string folderpath = "Mover/test.pdf";
            //string dfb_token = "sJL4Mu3FYOMAAAAAAAAApZIXTM1kfWsrpQ3_WF9wmmHke5szxNbTlWJa9-67SIzj";

            string uri = "https://api-content.dropbox.com/1/files/auto/" + folderpath; // +"?list=false";
            WebRequest request = WebRequest.Create(uri);

            request.Method = "GET";
            String authheader = "Authorization:Bearer " + dfb_token;
            request.Headers.Add(authheader);
            request.Headers.Add("X-Dropbox-Perform-As-Team-Member:" + dfb_member_id);

            try
            {
                Console.ForegroundColor = ConsoleColor.Green;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();

                // Open the stream using a StreamReader for easy access.
                //StreamReader reader = new StreamReader(dataStream);

                System.IO.FileStream output = new FileStream(output_file, FileMode.Create);
                dataStream.CopyTo(output);

                //reader.Close();
                dataStream.Close();
                response.Close();
                output.Close();

            }

            catch (WebException error)
            {
                Console.WriteLine(error);
            }

        }

    }
}
