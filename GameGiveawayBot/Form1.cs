using System.Globalization;
using System.Text;
using Newtonsoft.Json;


namespace GameGiveawayBot {
    public partial class Form1 : Form {
        private System.Windows.Forms.Timer dailyTimer;
        private string apiUrl;
        private string discordWebhookUrl;
        private string sentGiveawaysFile = "sent_giveaways.txt";

        public Form1() {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e) {
            notifyIcon1.Visible = false; // notify icon logic

            // Load filePath from config.json
            if (!LoadConfig()) {
                notifyIcon1.Visible = false;
                Application.Exit();
            }

            SetupDailyTimer();
        }

        // Method to load configuration from config.json
        private bool LoadConfig() {
            try {
                string configFilePath = "config.json"; // Path to the config.json file
                if (File.Exists(configFilePath)) {
                    string json = File.ReadAllText(configFilePath);
                    dynamic config = JsonConvert.DeserializeObject(json);
                    apiUrl = config.apiUrl;
                    discordWebhookUrl = config.discordWebhookUrl;

                    richTextBox1.Text = richTextBox1.Text + "apiurl : " + apiUrl + "\n\n";
                    richTextBox1.Text = richTextBox1.Text + "discordWebhookUrl : " + discordWebhookUrl + "\n";
                    return true;
                }
                else {
                    MessageBox.Show("Configuration file not found.");
                    return false;
                }
            }
            catch (Exception ex) {
                MessageBox.Show("Error loading configuration: " + ex.Message);
                return false;
            }
        }


        private void SetupDailyTimer() {
            dailyTimer = new System.Windows.Forms.Timer();
            dailyTimer.Interval = (int)GetNextInterval();  // Cast to int since it needs milliseconds as int
            dailyTimer.Tick += (sender, e) => {
                SendGiveaways();
                dailyTimer.Interval = (int)GetNextInterval(); // Reset interval after sending giveaways
            };
            dailyTimer.Start();
        }

        private double GetNextInterval() {
            var now = DateTime.Now;
            var nextRun = now.Date.AddHours(20);
            if (now > nextRun) nextRun = nextRun.AddDays(1);
            return (nextRun - now).TotalMilliseconds;
        }

        private void SendGiveawaysManually() {
            SendGiveaways();
        }

        private void SendGiveaways() {
            try {
                // Delete the save file if it is older than 3 months
                DeleteOldGiveAwaySaveFile(sentGiveawaysFile);

                using (var client = new HttpClient()) {
                    var response = client.GetStringAsync(apiUrl).Result; // Synchronously wait for the response
                    var giveaways = JsonConvert.DeserializeObject<List<Giveaway>>(response);

                    if (giveaways == null || giveaways.Count == 0) {
                        notifyIcon1.BalloonTipTitle = "No Giveaways Found";
                        notifyIcon1.BalloonTipText = "No active giveaways available.";
                        notifyIcon1.ShowBalloonTip(30000);
                        return;
                    }
                    richTextBox1.Text = richTextBox1.Text + $"giveaway count : {giveaways.Count}\n";
                    var newGiveaways = FilterNewGiveaways(giveaways);

                    if (newGiveaways.Count > 0) {
                        foreach (var giveaway in newGiveaways) {
                            SendToDiscord(giveaway);
                        }
                    }
                    else {
                        notifyIcon1.ShowBalloonTip(5000, "No New Giveaways", "All giveaways have been sent already.", ToolTipIcon.Info);
                    }
                }
            }
            catch (Exception ex) {
                notifyIcon1.ShowBalloonTip(5000, "Error", "Failed to send giveaways: " + ex.Message, ToolTipIcon.Error);
            }
        }

        private List<Giveaway> FilterNewGiveaways(List<Giveaway> giveaways) {
            var sentGiveawayIds = LoadSentGiveawayIds();
            var newGiveaways = new List<Giveaway>();

            foreach (var giveaway in giveaways) {
                if (!sentGiveawayIds.Contains(giveaway.id) && giveaway.status == "Active") {
                    newGiveaways.Add(giveaway);
                }
            }

            return newGiveaways;
        }

        private void SendToDiscord(Giveaway giveaway) {
            var msg = new {
                embeds = new[] {
                    new {
                        title = giveaway.title,
                        color = 1127128,
                        url = giveaway.open_giveaway_url,
                        description = $"Worth = {giveaway.worth}       EndDate: {RemoveLastThreeCharacters(giveaway.end_date)}\n" +
                                      $"Platforms: {giveaway.platforms}\n" +
                                      $"[GamerPower]({giveaway.gamerpower_url})       **[Get it HERE.]({giveaway.open_giveaway_url})**",
                        image = new { url = giveaway.image }
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(msg), Encoding.UTF8, "application/json");

            using (var client = new HttpClient()) {
                var response = client.PostAsync(discordWebhookUrl, content).Result; // Synchronously wait for the response
                if (!response.IsSuccessStatusCode) {
                    throw new Exception("Failed to send giveaway to Discord");
                }
                SaveGiveawayId(giveaway.id);
            }
        }

        private HashSet<int> LoadSentGiveawayIds() {
            var sentGiveaways = new HashSet<int>();

            if (File.Exists(sentGiveawaysFile)) {
                var lines = File.ReadAllLines(sentGiveawaysFile);
                foreach (var line in lines) {
                    if (int.TryParse(line, out var id)) {
                        sentGiveaways.Add(id);
                    }
                }
            }

            return sentGiveaways;
        }

        private void SaveGiveawayId(int id) {
            using (var writer = new StreamWriter(sentGiveawaysFile, true)) {
                writer.WriteLine(id);
            }
        }

        //public static string ConvertDateFormat(string dateStr) {
        //    DateTime dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        //    return dateTime.ToString("dd.MM.yyyy HH:mm");
        //}
        public static string RemoveLastThreeCharacters(string input) {
            if (input.Length <= 3 || !input.Contains(':')) {
                // If the string is 3 characters or shorter, return an empty string
                return input;
            }

            // Return the string without the last 3 characters
            return input.Substring(0, input.Length - 3);
        }

        public static void DeleteOldGiveAwaySaveFile(string filePath = "sent_giveaways.txt") {
            try {
                if (!File.Exists(filePath)) {
                    return;
                }

                DateTime currentDate = DateTime.Now;
                DateTime creationDate = File.GetCreationTime(filePath);

                if (currentDate - creationDate > TimeSpan.FromDays(90)) {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex) {
                // Handle errors during file deletion
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            SendGiveaways();
        }

        #region notifyIcon
        //add this to form load -> notifyIcon1.Visible = false; // notify icon logic
        private void Form1_Resize(object sender, EventArgs e) {
            if (this.WindowState == FormWindowState.Minimized) {
                Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            notifyIcon1.Visible = false;
            Application.Exit();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e) {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        #endregion


    }

    public struct Giveaway {
        public int id { get; set; }
        public string title { get; set; }
        public string status { get; set; }
        public string platforms { get; set; }
        public string image { get; set; }
        public string open_giveaway_url { get; set; }
        public string gamerpower_url { get; set; }
        public string worth { get; set; }
        public string end_date { get; set; }
    }
}
