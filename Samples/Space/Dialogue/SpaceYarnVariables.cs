using YarnSpinnerGodot;

[System.CodeDom.Compiler.GeneratedCode("YarnSpinner", "0.3.0")]
public partial class SpaceYarnVariables : YarnSpinnerGodot.InMemoryVariableStorage, YarnSpinnerGodot.IGeneratedVariableStorage {
    // Accessor for String $sampleName
    public string SampleName {
        get => this.GetValueOrDefault<string>("$sampleName");
        set => this.SetValue<string>("$sampleName", value);
    }

    // Accessor for Number $times_talked_sally_before_ship
    public float TimesTalkedSallyBeforeShip {
        get => this.GetValueOrDefault<float>("$times_talked_sally_before_ship");
        set => this.SetValue<float>("$times_talked_sally_before_ship", value);
    }

    // Accessor for Bool $player_avoided_ship
    public bool PlayerAvoidedShip {
        get => this.GetValueOrDefault<bool>("$player_avoided_ship");
    }

    // Accessor for Bool $apologized_to_sally
    public bool ApologizedToSally {
        get => this.GetValueOrDefault<bool>("$apologized_to_sally");
        set => this.SetValue<bool>("$apologized_to_sally", value);
    }

    // Accessor for Bool $should_see_ship
    /// <summary>
    /// Ship.yarn, node Ship, line 14
    /// </summary>
    public bool ShouldSeeShip {
        get => this.GetValueOrDefault<bool>("$should_see_ship");
        set => this.SetValue<bool>("$should_see_ship", value);
    }

    // Accessor for Bool $sally_warning
    /// <summary>
    /// Ship.yarn, node Ship, line 14
    /// </summary>
    public bool SallyWarning {
        get => this.GetValueOrDefault<bool>("$sally_warning");
        set => this.SetValue<bool>("$sally_warning", value);
    }

}
