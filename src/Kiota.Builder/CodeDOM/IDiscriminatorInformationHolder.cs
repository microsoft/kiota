namespace Kiota.Builder;

public interface IDiscriminatorInformationHolder {
    /// <summary>
    /// Provides information to discriminate the payload response into the correct member type.
    /// </summary>
    public DiscriminatorInformation DiscriminatorInformation { get; set; }
}
