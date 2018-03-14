using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using IC = InControl;
using UtilityExtensions;


public class GameModel : MonoBehaviour {

    public static GameModel instance;
    public ScoreDisplayer scoreDisplayer;
    public NamedColor[] teamColors;
    public TeamManager[] teams { get; set; }
    public int scoreMax = 7;
    public SceneStateController scene_controller {get; set;}
    public GameEndController end_controller {get; set;}
    public float matchLength = 5f;
    public NotificationCenter nc;

    float matchLengthSeconds;

    IntCallback NextTeamAssignmentIndex;

    void Awake() {
        if (instance == null) {
            instance = this;
            Initialization();
        }
        else {
            Destroy(gameObject);
        }
    }

    void Initialization() {
        InitializeTeams();
        scene_controller = GetComponent<SceneStateController>();
        end_controller = GetComponent<GameEndController>();
        matchLengthSeconds = 60 * matchLength;
        this.TimeDelayCall(EndGame, matchLengthSeconds);
        nc = new NotificationCenter();
    }

    void Start() {
        scoreDisplayer.StartMatchLengthUpdate(matchLengthSeconds);
    }

    void EndGame() {
        Debug.Log("game over");
        var winner = teams.Aggregate(
            (best, next) => {
                if (best == null || best.score == next.score) {
                    return null;
                }
                return next.score > best.score ? next : best;
            });
        end_controller.GameOver(winner);
    }


    public TeamManager GetTeamAssignment(Player caller) {
        var assignedTeam = teams[NextTeamAssignmentIndex()];
        assignedTeam.AddTeamMember(caller);
        return assignedTeam;
    }

    void InitializeTeams() {
        teams = new TeamManager[teamColors.Length];

        for (int i = 0; i < teamColors.Length; ++i) {
            // Add 1 so we get Team 1 and Team 2
            teams[i] = new TeamManager(i + 1, teamColors[i]);
        }
        NextTeamAssignmentIndex = Utility.ModCycle(0, teams.Length);
    }

    public void Scored(TeamManager team) {
        // One team just scored
        //
        // TODO: handle things like resetting the ball and players here, maybe
        // show UI elements

    }
}
