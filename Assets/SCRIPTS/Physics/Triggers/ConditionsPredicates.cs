using UnityEngine;

public class ConditionsPredicates
{

    #region Conditions
    
    public static bool ConditionsEnterPlayer(Collider sender)
    {
        Debug.LogError("ConditionsEnterPlayer TODO");
        return true;
        //var pc = PlayerController.I;
        //return (PlayerController.Can) && sender.IsPlayer() && (!pc.PlayerTarget.VehicleControl.CharInVehicle) && (pc.PlayerTarget.LifeControl.Lived);
    }

    #endregion
}
