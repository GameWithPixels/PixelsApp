using Newtonsoft.Json;
using Systemic.Unity.Pixels;

public delegate void EditDieEventHandler(EditDie editDie);

[System.Serializable]
public class EditDie
{
    public string name;
    //public ulong deviceId;
    public string systemId;
    public int ledCount; // Which kind of dice this is
    public PixelDesignAndColor designAndColor; // Physical look
    public int currentBehaviorIndex;

    [JsonIgnore]
    public EditProfile currentBehavior;

    [JsonIgnore]
    public Pixel die { get; private set; }

    [JsonIgnore]
    public EditDieEventHandler onDieFound;
    [JsonIgnore]
    public EditDieEventHandler onDieWillBeLost;

    public void SetDie(Pixel die)
    {
        if (this.die != die)
        {
            if (this.die != null)
            {
                onDieWillBeLost?.Invoke(this);
            }
            this.die = die;
            if (this.die != null)
            {
                // We should check die information (name, design, hash)
                onDieFound?.Invoke(this);
            }
        }
    }

    public void OnBeforeSerialize()
    {
        currentBehaviorIndex = AppDataSet.Instance.profiles.IndexOf(currentBehavior);
    }

    public void OnAfterDeserialize()
    {
        if (currentBehaviorIndex >= 0 && currentBehaviorIndex < AppDataSet.Instance.profiles.Count)
            currentBehavior = AppDataSet.Instance.profiles[currentBehaviorIndex];
        else
            currentBehavior = null;
    }
}
