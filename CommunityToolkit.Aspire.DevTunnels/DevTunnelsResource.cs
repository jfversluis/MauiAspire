namespace CommunityToolkit.Aspire.DevTunnels;

public class DevTunnelsResource : IResource
{
    public string Name => "Dev Tunnels";

    public ResourceAnnotationCollection Annotations { get; set; } = [];
}
