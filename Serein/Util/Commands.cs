using Foster.Framework;
using System.Globalization;
using System.Text;

namespace Serein;

public class Commands
{
    private const float UNDERSCORE_TIME = .5f;
    private const float OPACITY = .8f;

    private const Keys ToggleKey = Keys.Tilde;

    public bool Enabled = true;
    public bool Open;
    public Action[] FunctionKeyActions { get; private set; }

    private Dictionary<string, CommandInfo> commands;
    private List<string> sorted;

    private string currentText = "";
    private List<Line> drawCommands;
    private bool underscore;
    private float underscoreCounter;
    private List<string> commandHistory;
    private int seekIndex = -1;
    private int tabIndex = -1;
    private string tabSearch;
    private bool canOpen;

    public Commands()
    {
        commandHistory = new List<string>();
        drawCommands = new List<Line>();
        commands = new Dictionary<string, CommandInfo>();
        sorted = new List<string>();
        FunctionKeyActions = new Action[12];

        RegisterBuiltIns();
    }

    public void Log(object obj, Color color)
    {
        string str = obj.ToString();

        //Newline splits
        if (str.Contains('\n'))
        {
            foreach (var line in str.Split('\n'))
                Log(line.TrimEnd('\r'), color);
            return;
        }

        //Split the string if you overlow horizontally
        int maxWidth = Engine.Instance.Window.WidthInPixels - 40;
        while (Draw.DefaultFont.SizeOf(str).X > maxWidth)
        {
            int split = -1;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == ' ')
                {
                    if (Draw.DefaultFont.SizeOf(str.Substring(0, i)).X <= maxWidth)
                        split = i;
                    else
                        break;
                }
            }

            if (split == -1)
                break;

            drawCommands.Insert(0, new Line(str.Substring(0, split), color));
            str = str.Substring(split + 1);
        }

        drawCommands.Insert(0, new Line(str, color));

        //Don't overflow top of window
        int maxCommands = (Engine.Instance.Window.HeightInPixels - 100) / 30;
        while (drawCommands.Count > maxCommands)
            drawCommands.RemoveAt(drawCommands.Count - 1);
    }

    public void Log(object obj)
    {
        Log(obj, Color.White);
    }

    #region Updating and Rendering

    internal void UpdateClosed()
    {
        var keyboard = Engine.Instance.Input.Keyboard;

        if (!canOpen)
            canOpen = true;
        else if (keyboard.Pressed(ToggleKey))
            Open = true;

        for (int i = 0; i < FunctionKeyActions.Length; i++)
            if (keyboard.Pressed(Keys.F1 + i))
                ExecuteFunctionKeyAction(i);
    }

    internal void UpdateOpen()
    {
        var keyboard = Engine.Instance.Input.Keyboard;

        underscoreCounter += Engine.Instance.Time.Delta;
        while (underscoreCounter >= UNDERSCORE_TIME)
        {
            underscoreCounter -= UNDERSCORE_TIME;
            underscore = !underscore;
        }

        //Close before reading text, so the toggle key's own character never lands in the prompt
        if (keyboard.Pressed(ToggleKey))
        {
            Open = canOpen = false;
            return;
        }

        //Typed characters come from the OS text input, so keyboard layout, Shift/AltGr,
        //dead keys and IMEs are all handled for us. No key -> character tables needed.
        foreach (var c in keyboard.Text.ToString())
        {
            if (char.IsControl(c))
                continue;

            currentText += c;
            tabIndex = -1;
        }

        //Editing keys. Repeated() fires on the initial press and again on the OS key repeat.
        if (keyboard.Repeated(Keys.Backspace) && currentText.Length > 0)
        {
            //Don't split a surrogate pair in half
            int remove = currentText.Length >= 2 && char.IsLowSurrogate(currentText[^1]) && char.IsHighSurrogate(currentText[^2]) ? 2 : 1;
            currentText = currentText.Substring(0, currentText.Length - remove);
            tabIndex = -1;
        }

        if (keyboard.Pressed(Keys.Delete))
        {
            currentText = "";
            tabIndex = -1;
        }

        //Command history
        if (keyboard.Repeated(Keys.Up) && seekIndex < commandHistory.Count - 1)
        {
            seekIndex++;
            currentText = commandHistory[seekIndex];
            tabIndex = -1;
        }

        if (keyboard.Repeated(Keys.Down) && seekIndex > -1)
        {
            seekIndex--;
            currentText = seekIndex == -1 ? "" : commandHistory[seekIndex];
            tabIndex = -1;
        }

        //Tab completion (Shift+Tab goes backwards)
        if (keyboard.Repeated(Keys.Tab))
        {
            if (keyboard.Down(Keys.LeftShift) || keyboard.Down(Keys.RightShift))
            {
                if (tabIndex == -1)
                {
                    tabSearch = currentText;
                    FindLastTab();
                }
                else
                {
                    tabIndex--;
                    if (tabIndex < 0 || !TabMatches(tabIndex))
                        FindLastTab();
                }
            }
            else
            {
                if (tabIndex == -1)
                {
                    tabSearch = currentText;
                    FindFirstTab();
                }
                else
                {
                    tabIndex++;
                    if (tabIndex >= sorted.Count || !TabMatches(tabIndex))
                        FindFirstTab();
                }
            }

            if (tabIndex != -1)
                currentText = sorted[tabIndex];
        }

        for (int i = 0; i < FunctionKeyActions.Length; i++)
            if (keyboard.Pressed(Keys.F1 + i))
                ExecuteFunctionKeyAction(i);

        if (keyboard.Pressed(Keys.Enter) && currentText.Length > 0)
        {
            tabIndex = -1;
            EnterCommand();
        }
    }

    private void EnterCommand()
    {
        string[] data = currentText.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (commandHistory.Count == 0 || commandHistory[0] != currentText)
            commandHistory.Insert(0, currentText);
        drawCommands.Insert(0, new Line(currentText, Color.Aqua));
        currentText = "";
        seekIndex = -1;

        string[] args = new string[data.Length - 1];
        for (int i = 1; i < data.Length; i++)
            args[i - 1] = data[i];
        ExecuteCommand(data[0].ToLowerInvariant(), args);
    }

    private bool TabMatches(int index)
    {
        return tabSearch == "" || sorted[index].StartsWith(tabSearch, StringComparison.Ordinal);
    }

    private void FindFirstTab()
    {
        for (int i = 0; i < sorted.Count; i++)
        {
            if (TabMatches(i))
            {
                tabIndex = i;
                break;
            }
        }
    }

    private void FindLastTab()
    {
        for (int i = 0; i < sorted.Count; i++)
            if (TabMatches(i))
                tabIndex = i;
    }

    internal void Render()
    {
        int screenWidth = Engine.Instance.Window.WidthInPixels;
        int screenHeight = Engine.Instance.Window.HeightInPixels;
        var background = Color.Black * OPACITY;

        Draw.Batcher.Rect(new Rect(10, screenHeight - 50, screenWidth - 20, 40), background);
        Draw.Batcher.Text(Draw.DefaultFont, underscore ? $">{currentText}_" : $">{currentText}", new Vector2(20, screenHeight - 42), Color.White);

        if (drawCommands.Count > 0)
        {
            int height = 10 + (30 * drawCommands.Count);
            Draw.Batcher.Rect(new Rect(10, screenHeight - height - 60, screenWidth - 20, height), background);
            for (int i = 0; i < drawCommands.Count; i++)
                Draw.Batcher.Text(Draw.DefaultFont, drawCommands[i].Text, new Vector2(20, screenHeight - 92 - (30 * i)), drawCommands[i].Color);
        }
    }

    #endregion

    #region Execute

    public void ExecuteCommand(string command, string[] args)
    {
        if (!commands.TryGetValue(command, out var info))
        {
            Log("Command '" + command + "' not found! Type 'help' for list of commands", Color.Yellow);
            return;
        }

        try
        {
            info.Action(new CommandArgs(args));
        }
        catch (Exception e)
        {
            Log(e.Message, Color.Yellow);
            LogStackTrace(e.StackTrace);
        }
    }

    public void ExecuteFunctionKeyAction(int num)
    {
        if (FunctionKeyActions[num] != null)
            FunctionKeyActions[num]();
    }

    #endregion

    #region Registering Commands

    //Commands are registered explicitly instead of being discovered through reflection
    //(scanning assemblies for [Command] methods and Invoke()-ing them isn't trim / Native AOT safe).
    //
    //  Engine.Commands.Register("spawn", "Spawns an enemy", "[count:int=1]",
    //      args => Spawn(args.Int(0, 1)));

    public void Register(string name, string help, Action<CommandArgs> action)
    {
        Register(name, help, "", action);
    }

    /// <param name="usage">Shown by 'help', e.g. "[count:int=1 name:string]"</param>
    public void Register(string name, string help, string usage, Action<CommandArgs> action)
    {
        //Input is lowercased before lookup, so the name has to be as well
        name = name.ToLowerInvariant();

        commands[name] = new CommandInfo { Action = action, Help = help, Usage = usage };

        //Keep the tab-completion list sorted and free of duplicates
        int index = sorted.BinarySearch(name, StringComparer.Ordinal);
        if (index < 0)
            sorted.Insert(~index, name);
    }

    private void LogStackTrace(string stackTrace)
    {
        //Works from the plain trace text, because that is what is available under Native AOT
        //(StackFrame.GetMethod() and file info aren't)
        if (string.IsNullOrEmpty(stackTrace))
            return;

        foreach (var call in stackTrace.Split('\n'))
        {
            string log = call.Trim();
            if (log.Length == 0)
                continue;

            //Remove file path, keeping just the file name. Handles both / and \ separators,
            //since the path is whatever the build machine used.
            int inFile = log.LastIndexOf(" in ", StringComparison.Ordinal);
            if (inFile != -1)
            {
                int pathStart = inFile + 4;
                int separator = log.LastIndexOfAny(PathSeparators);
                if (separator >= pathStart)
                    log = log.Remove(pathStart, separator + 1 - pathStart);
            }

            //Remove arguments list
            int open = log.IndexOf('(');
            int close = log.IndexOf(')');
            if (open != -1 && close > open)
                log = log.Remove(open + 1, close - open - 1);

            //Space out the colon line number
            if (inFile != -1)
            {
                int colon = log.LastIndexOf(':');
                if (colon != -1)
                    log = log.Insert(colon + 1, " ").Insert(colon, " ");
            }

            Log("-> " + log, Color.White);
        }
    }

    private static readonly char[] PathSeparators = { '/', '\\' };

    private struct CommandInfo
    {
        public Action<CommandArgs> Action;
        public string Help;
        public string Usage;
    }

    #endregion

    #region Built-In Commands

    private void RegisterBuiltIns()
    {
        Register("clear", "Clears the terminal", args => Clear());
        Register("exit", "Exits the game", args => Exit());
        Register("vsync", "Enables or disables vertical sync", "[enabled:bool=true]", args => Vsync(args.Bool(0, true)));
        Register("unlocked", "Disables fixed time step", args => Unlocked());
        Register("framerate", "Sets fixed time step and the target framerate", "[targetFps:int]", args => Framerate(args.Int(0)));
        Register("count", "Logs amount of Entities in the Scene. Pass a tagIndex to count only Entities with that tag", "[tagIndex:int=-1]", args => Count(args.Int(0, -1)));
        Register("tracker", "Logs all tracked objects in the scene. Set mode to 'e' for just entities, 'c' for just components, or 'cc' for just collidable components", "[mode:string]", args => Tracker(args.String(0)));
        Register("pooler", "Logs the pooled Entity counts", args => Pooler());
        Register("fullscreen", "Switches to fullscreen mode", args => Fullscreen());
        Register("window", "Switches to window mode", "[scale:int=1]", args => Window(args.Int(0, 1)));
        Register("help", "Shows usage help for a given command", "[command:string]", args => Help(args.String(0)));
    }

    public static void Clear()
    {
        Engine.Commands.drawCommands.Clear();
    }

    private static void Exit()
    {
        Engine.Instance.Exit();
    }

    private static void Vsync(bool enabled = true)
    {
        Engine.Instance.GraphicsDevice.VSync = enabled;
        Engine.Commands.Log("Vertical Sync " + (enabled ? "Enabled" : "Disabled"));
    }

    private static void Unlocked()
    {
        Engine.Instance.UpdateMode = UpdateMode.UnlockedStep();
        Engine.Commands.Log("Unlocked Time Step.");
    }

    private static void Framerate(int targetFps)
    {
        Engine.Instance.UpdateMode = UpdateMode.FixedStep(targetFps);
        Engine.Commands.Log("Fixed Time Step " + targetFps + "fps");
    }

    private static void Count(int tagIndex = -1)
    {
        if (Engine.Scene == null)
        {
            Engine.Commands.Log("Current Scene is null!");
            return;
        }

        if (tagIndex < 0)
            Engine.Commands.Log(Engine.Scene.Entities.Count.ToString());
        else
            Engine.Commands.Log(Engine.Scene.TagLists[tagIndex].Count.ToString());
    }

    private static void Tracker(string mode)
    {
        if (Engine.Scene == null)
        {
            Engine.Commands.Log("Current Scene is null!");
            return;
        }

        switch (mode)
        {
            default:
                Engine.Commands.Log("-- Entities --");
                Engine.Scene.Tracker.LogEntities();
                Engine.Commands.Log("-- Components --");
                Engine.Scene.Tracker.LogComponents();
                Engine.Commands.Log("-- Collidable Components --");
                Engine.Scene.Tracker.LogCollidableComponents();
                break;

            case "e":
                Engine.Scene.Tracker.LogEntities();
                break;

            case "c":
                Engine.Scene.Tracker.LogComponents();
                break;

            case "cc":
                Engine.Scene.Tracker.LogCollidableComponents();
                break;
        }
    }

    private static void Pooler()
    {
        Engine.Pooler.Log();
    }

    private static void Fullscreen()
    {
        Engine.Instance.Window.Fullscreen = true;
    }

    private static void Window(int scale = 1)
    {
        Engine.Instance.Window.Fullscreen = false;
    }

    private static void Help(string command)
    {
        if (Engine.Commands.sorted.Contains(command))
        {
            var c = Engine.Commands.commands[command];
            StringBuilder str = new StringBuilder();

            //Title
            str.Append(":: ");
            str.Append(command);

            //Usage
            if (!string.IsNullOrEmpty(c.Usage))
            {
                str.Append(" ");
                str.Append(c.Usage);
            }
            Engine.Commands.Log(str.ToString());

            //Help
            if (string.IsNullOrEmpty(c.Help))
                Engine.Commands.Log("No help info set");
            else
                Engine.Commands.Log(c.Help);
        }
        else
        {
            StringBuilder str = new StringBuilder();
            str.Append("Commands list: ");
            str.Append(string.Join(", ", Engine.Commands.sorted));
            Engine.Commands.Log(str.ToString());
            Engine.Commands.Log("Type 'help command' for more info on that command!");
        }
    }
    #endregion

    /// <summary>
    /// The arguments typed after a command name. Missing arguments give the supplied fallback;
    /// present-but-unparsable ones give 0 / false. Parsing is culture-invariant ('.' decimals).
    /// </summary>
    public readonly struct CommandArgs
    {
        private readonly string[] values;

        public CommandArgs(string[] values)
        {
            this.values = values ?? Array.Empty<string>();
        }

        public int Count => values?.Length ?? 0;

        public string String(int index, string fallback = "")
        {
            return index < Count ? values[index] ?? "" : fallback;
        }

        public bool Bool(int index, bool fallback = false)
        {
            if (index >= Count)
                return fallback;

            var arg = values[index];
            if (arg == null)
                return false;

            return !(arg == "0"
                || arg.Equals("false", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("f", StringComparison.OrdinalIgnoreCase));
        }

        public int Int(int index, int fallback = 0)
        {
            if (index >= Count)
                return fallback;
            return int.TryParse(values[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
        }

        public float Float(int index, float fallback = 0)
        {
            if (index >= Count)
                return fallback;
            return float.TryParse(values[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;
        }
    }

    private struct Line
    {
        public string Text;
        public Color Color;

        public Line(string text)
        {
            Text = text;
            Color = Color.White;
        }

        public Line(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }
}

[Obsolete("Reflection-based command discovery isn't Native AOT safe. Call Engine.Commands.Register(...) instead.", true)]
public class Command : Attribute
{
    public Command(string name, string help)
    {
    }
}