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
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            //temporary for testing:
            //txtToken.Text = "5POVRTzm3ZAAAAAAAAABwcZwXMCSEGQepWk7GDKjb_1yr_C7xMifXgE7QP7kv7Pi"; //dbtests.info
            txtToken.Text = "32Wp8V0dZ28AAAAAAADJlETVJXSVl4MdK_vjaDRxqFZ8_Id_qdbZPt0JoTOmfglU"; //hanfordinc.com

            pictureBoxConnected.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxConnected.Image = imageList1.Images[1];

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
                pictureBoxConnected.Image = imageList1.Images[0];
                populateDfBMembers(dfbTeam);
            }


            // Set cursor as default arrow
            Cursor.Current = Cursors.Default;    
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


    }
}
