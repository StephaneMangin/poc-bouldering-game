using Project.Core.Input;
using Project.UI;
using UnityEngine;

namespace Project.App
{
    /// <summary>
    /// Screens of the game (title, climbing, problem list, pause, controls), their keys and what
    /// the climber may do on each: gameplay input only flows while climbing.
    /// </summary>
    [RequireComponent(typeof(GameBootstrap))]
    public sealed class GameFlow : MonoBehaviour
    {
        private enum Screen
        {
            Title,
            Playing,
            Browser,
            Paused,
            Controls,
        }

        /// <summary>Starts directly on a problem: -problem quinquin-2 (scripted playtests, direct links).</summary>
        private const string ProblemArgument = "-problem";

        private static readonly Vector3 ShowcasePosition = new(9f, 3.2f, -9.5f);
        private static readonly Vector3 ShowcaseTarget = new(-2f, 2.2f, 5f);

        private readonly ProblemBrowser _browser = new();
        private GameBootstrap _game;
        private Screen _screen = Screen.Title;
        private Screen _afterControls = Screen.Title;
        private TitleActions _title;
        private PauseActions _pause;
        private BrowserActions _browserActions;
        private HudSources _hud;

        private InputReader Input => _game.Climber.GetComponent<InputReader>();

        private void Start()
        {
            _game = GetComponent<GameBootstrap>();
            _browser.HoldColor = _game.HoldColor;
            BuildActions();
            ShowTitle();
            var problem = _game.Content.ProblemById(CommandLineValue(ProblemArgument) ?? string.Empty);
            if (problem != null)
            {
                Play(() => _game.Session.Select(problem));
            }
        }

        /// <summary>Value following a command line flag, null when absent.</summary>
        private static string CommandLineValue(string flag)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == flag)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private void Update()
        {
            var input = Input;
            switch (_screen)
            {
                case Screen.Title when input.ConsumeConfirmPressed():
                    Show(Screen.Playing);
                    break;
                case Screen.Playing when input.ConsumePausePressed():
                    Show(Screen.Paused);
                    break;
                case Screen.Playing when input.ConsumeBrowserPressed():
                    OpenBrowser();
                    break;
                case Screen.Playing when input.ConsumeHelpPressed():
                    ShowControls(Screen.Playing);
                    break;
                case Screen.Browser when input.ConsumeBrowserPressed() || input.ConsumePausePressed():
                case Screen.Paused when input.ConsumePausePressed():
                case Screen.Controls when input.ConsumePausePressed():
                    Show(_screen == Screen.Controls ? _afterControls : Screen.Playing);
                    break;
            }
        }

        private void OnGUI()
        {
            switch (_screen)
            {
                case Screen.Title:
                    MenuScreens.DrawTitle(_title);
                    break;
                case Screen.Playing:
                    GameHud.Draw(_hud);
                    break;
                case Screen.Browser:
                    _browser.Draw(_game.Content, _game.Session.Records, _browserActions);
                    break;
                case Screen.Paused:
                    MenuScreens.DrawPause(_pause);
                    break;
                default:
                    ControlsPanel.Draw(() => Show(_afterControls));
                    break;
            }
        }

        private void BuildActions()
        {
            _title = new TitleActions
            {
                Climb = () => Show(Screen.Playing),
                SwitchClimber = () =>
                {
                    _game.SwitchClimber();
                    _hud = Hud();
                },
                ShowControls = () => ShowControls(Screen.Title),
                Quit = Application.Quit,
                IsReady = () => true,
                ClimberName = () => _game.ClimberName,
                Subtitle = () => $"<color=#EB5F38>bouldering</color>  ·  {_game.Content.ProblemCount} problems on {_game.Content.Sectors.Count} sectors",
            };
            _pause = new PauseActions
            {
                Resume = () => Show(Screen.Playing),
                Restart = () => Play(_game.Session.GoToStart),
                LeaveProblem = () => Play(_game.Session.Leave),
                OpenProblems = OpenBrowser,
                ShowControls = () => ShowControls(Screen.Paused),
                CyclePass = _game.CyclePass,
                Quit = Application.Quit,
                HasProblem = () => _game.Session.Session.Problem != null,
                PassName = () => _game.Pass.ToString(),
            };
            _browserActions = new BrowserActions
            {
                Select = problem => Play(() => _game.Session.Select(problem)),
                FreeClimb = () => Play(_game.Session.Leave),
                Close = () => Show(Screen.Playing),
            };
            _hud = Hud();
        }

        private HudSources Hud()
        {
            return new HudSources
            {
                Climber = _game.Climber,
                Session = _game.Session,
                Aim = _game.Aim,
                SectorName = () => _game.NearestSector(_game.Climber.transform.position)?.Name,
                HoldColor = _game.HoldColor,
            };
        }

        private void Play(System.Action action)
        {
            action();
            Show(Screen.Playing);
        }

        private void OpenBrowser()
        {
            _browser.Open(_game.NearestSector(_game.Climber.transform.position));
            Show(Screen.Browser);
        }

        private void ShowControls(Screen returnTo)
        {
            _afterControls = returnTo;
            Show(Screen.Controls);
        }

        private void ShowTitle()
        {
            _game.Orbit.enabled = false;
            _game.Orbit.transform.SetPositionAndRotation(ShowcasePosition, Quaternion.LookRotation(ShowcaseTarget - ShowcasePosition, Vector3.up));
            Show(Screen.Title);
        }

        private void Show(Screen screen)
        {
            if (_screen == Screen.Title && screen == Screen.Playing)
            {
                _game.Orbit.enabled = true;
                _game.Orbit.Snap(_game.Climber.transform.forward);
            }

            _screen = screen;
            _game.Climber.InputEnabled = screen == Screen.Playing;
        }
    }
}
