#region async
//using System.Globalization;
//using System.Text;
//using Newtonsoft.Json;


//namespace GameGiveawayBot {
//    public partial class Form1 : Form {
//        private System.Windows.Forms.Timer dailyTimer;
//        private string apiUrl = "https://www.gamerpower.com/api/filter?type=game&sort-by=date";
//        private string discordWebhookUrl = "https://media.guilded.gg/webhooks/84fcce50-845a-43c3-b0ac-9ab99be5647c/x4z1By05heo0YKI8s64OcUqGkuaAcyWOgSO4oqqWQ8eQGuo4oggSamgES06Em6CceYgesaa60SWSsUoUsukIsC";
//        private string sentGiveawaysFile = "sent_giveaways.txt";

//        public Form1() {
//            InitializeComponent();

//            // Set up the form as hidden
//            //this.WindowState = FormWindowState.Minimized;
//            //this.ShowInTaskbar = false;

//            // Set up system tray icon and daily timer
//            SetupSystemTray();
//            SetupDailyTimer();
//        }

//        // Initialize the system tray icon and context menu
//        private void SetupSystemTray() {
//            notifyIcon1.Visible = true;
//            notifyIcon1.ContextMenuStrip = new ContextMenuStrip();

//            // Add menu items to the tray icon context menu
//            notifyIcon1.ContextMenuStrip.Items.Add("Send Giveaways Now", null, (s, e) => SendGiveawaysManually());
//            notifyIcon1.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application.Exit());

//            notifyIcon1.ShowBalloonTip(1000, "Game Giveaway Bot", "Bot is running in the system tray.", ToolTipIcon.Info);
//        }

//        // Setup a daily timer for 20:00
//        private void SetupDailyTimer() {
//            dailyTimer = new System.Windows.Forms.Timer();
//            dailyTimer.Interval = (int)GetNextInterval();  // Cast to int since it needs milliseconds as int
//            dailyTimer.Tick += async (sender, e) => {
//                await SendGiveawaysAsync();
//                dailyTimer.Interval = (int)GetNextInterval(); // Reset interval after sending giveaways
//            };
//            dailyTimer.Start();
//        }

//        // Calculate the interval for the next run at 20:00
//        private double GetNextInterval() {
//            var now = DateTime.Now;
//            var nextRun = now.Date.AddHours(20);
//            if (now > nextRun) nextRun = nextRun.AddDays(1);
//            return (nextRun - now).TotalMilliseconds;
//        }

//        // Manually trigger the giveaway sending process
//        private async void SendGiveawaysManually() {
//            await SendGiveawaysAsync();
//        }

//        // MAIN Fetch giveaways from the API, filter out duplicates, and send to Discord
//        private async Task SendGiveawaysAsync() {
//            try {
//                // delete the save file if file 3 months old
//                await DeleteOldGiveAwaySaveFileAsync();
//                using (var client = new HttpClient()) {
//                    var response = await client.GetStringAsync(apiUrl);
//                    var giveaways = JsonConvert.DeserializeObject<List<Giveaway>>(response);
//                    if (giveaways == null) {
//                        notifyIcon1.BalloonTipTitle = "API No Giveaway";
//                        notifyIcon1.BalloonTipText = "API giveaway is null";
//                        notifyIcon1.ShowBalloonTip(30000);
//                        return;
//                    }
//                    var newGiveaways = FilterNewGiveaways(giveaways);

//                    if (newGiveaways.Count > 0) {
//                        foreach (var giveaway in newGiveaways) {
//                            await SendToDiscordAsync(giveaway);
//                            //SaveGiveawayId(giveaway.id);
//                        }
//                    }
//                }
//            }
//            catch (Exception ex) {
//                notifyIcon1.ShowBalloonTip(5000, "Error", "Failed to send giveaways: " + ex.Message, ToolTipIcon.Error);
//            }
//        }

//        // Check if giveaway IDs are duplicates and filter them out
//        private List<Giveaway> FilterNewGiveaways(List<Giveaway> giveaways) {
//            var sentGiveawayIds = LoadSentGiveawayIds();
//            var newGiveaways = new List<Giveaway>();

//            foreach (var giveaway in giveaways) {
//                if (giveaway.status == "Active" && !sentGiveawayIds.Contains(giveaway.id)) {
//                    newGiveaways.Add(giveaway);
//                }
//            }

//            return newGiveaways;
//        }

//        // Send giveaway details to the Discord channel
//        private async Task SendToDiscordAsync(Giveaway giveaway) {
//            //MessageBox.Show(giveaway.title);
//            var msg = new {
//                embeds = new[]
//                {
//                    new
//                    {
//                        title = giveaway.title,
//                        color = 1127128,
//                        url = giveaway.open_giveaway_url,
//                        description = $"Worth = {giveaway.worth}       EndDate:{ConvertDateFormat(giveaway.end_date)}\n" +
//                                    $"Platforms: {giveaway.platforms}\n" +
//                                    $"[GamerPower]({giveaway.gamerpower_url})       **[Get it HERE.]({giveaway.open_giveaway_url})**",
//                        image = new
//                        {
//                            url = giveaway.image
//                        }
//                    }
//                }
//            };

//            var content = new StringContent(JsonConvert.SerializeObject(msg), Encoding.UTF8, "application/json");
//            //string xx = await content.ReadAsStringAsync();
//            //MessageBox.Show(xx);

//            using (var client = new HttpClient()) {
//                var response = await client.PostAsync(discordWebhookUrl, content);
//                if (!response.IsSuccessStatusCode) {
//                    throw new Exception("Failed to send giveaway to Discord");
//                }
//                else {
//                    SaveGiveawayId(giveaway.id);
//                }
//            }
//        }

//        // Load the list of already sent giveaway IDs from a file
//        private HashSet<int> LoadSentGiveawayIds() {
//            var sentGiveaways = new HashSet<int>();

//            if (File.Exists(sentGiveawaysFile)) {
//                var lines = File.ReadAllLines(sentGiveawaysFile);
//                foreach (var line in lines) {
//                    if (int.TryParse(line, out var id)) {
//                        sentGiveaways.Add(id);
//                    }
//                }
//            }

//            return sentGiveaways;
//        }

//        // Save a giveaway ID to prevent duplicates
//        private void SaveGiveawayId(int id) {
//            if (!File.Exists(sentGiveawaysFile)) {
//                File.Create(sentGiveawaysFile);
//            }
//            using (var writer = File.AppendText(sentGiveawaysFile)) {
//                writer.WriteLine(id);
//            }
//        }

//        public static string ConvertDateFormat(string dateStr) {
//            // Parse the original date string
//            DateTime dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

//            // Format the date to the desired output format
//            return dateTime.ToString("dd.MM.yyyy HH:mm");
//        }

//        public static async Task DeleteOldGiveAwaySaveFileAsync(string filePath = "sent_giveaways.txt") {
//            try {
//                // Get the current directory (where the executable is located)
//                //string directoryPath = AppDomain.CurrentDomain.BaseDirectory;

//                // Construct the full file path
//                //string filePath = Path.Combine(directoryPath, fileName);

//                // Check if the file exists
//                if (!File.Exists(filePath)) {
//                    Console.WriteLine($"File '{filePath}' does not exist.");
//                    return;
//                }

//                // Get the current date
//                DateTime currentDate = DateTime.Now;

//                // Get the creation date of the file
//                DateTime creationDate = File.GetCreationTime(filePath);

//                // Check if the file is older than 3 months
//                if (currentDate - creationDate > TimeSpan.FromDays(90)) {
//                    // Delete the file asynchronously
//                    await Task.Run(() => File.Delete(filePath));
//                    Console.WriteLine($"Deleted: {filePath}");
//                }
//                else {
//                    Console.WriteLine($"File '{filePath}' is not older than 3 months.");
//                }

//                Console.WriteLine("Process completed.");
//            }
//            catch (Exception ex) {
//                Console.WriteLine($"An error occurred: {ex.Message}");
//            }
//        }

//        private async void button1_Click(object sender, EventArgs e) {
//            await SendGiveawaysAsync();
//        }
//    }

//    public struct Giveaway {
//        public int id { get; set; }
//        public string  title { get; set; }
//        public string status { get; set; }
//        public string platforms { get; set; }
//        public string image { get; set; }
//        public string open_giveaway_url { get; set; }
//        public string gamerpower_url { get; set; }
//        public string worth { get; set; }
//        public string end_date { get; set; }
//    }
//}

#endregion
using System.Globalization;
using System.Text;
using Newtonsoft.Json;


namespace GameGiveawayBot {
    public partial class Form1 : Form {
        private System.Windows.Forms.Timer dailyTimer;
        private string apiUrl = "https://www.gamerpower.com/api/filter?type=game&sort-by=date";
        private string discordWebhookUrl = "https://media.guilded.gg/webhooks/e31e10c1-1fe7-487d-9b6d-14bf3c55943d/imSelWpA3YKEGCumoEqsywMUC0Scyys0WKGKEQ8OceqeumYOeOG8aWW2i6kYeAG0mECYeWUEwaIOCEGCikIYYk";
        private string sentGiveawaysFile = "sent_giveaways.txt";

        public Form1() {
            InitializeComponent();
            SetupSystemTray();
            SetupDailyTimer();
        }

        private void SetupSystemTray() {
            notifyIcon1.Visible = true;
            notifyIcon1.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon1.ContextMenuStrip.Items.Add("Send Giveaways Now", null, (s, e) => SendGiveawaysManually());
            notifyIcon1.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application.Exit());
            notifyIcon1.ShowBalloonTip(1000, "Game Giveaway Bot", "Bot is running in the system tray.", ToolTipIcon.Info);
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
                    richTextBox1.Text = richTextBox1.Text + $"giveaway count : {giveaways.Count}";
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
