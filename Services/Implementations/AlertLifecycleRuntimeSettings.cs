using Microsoft.Extensions.Configuration;

namespace DaNangSafeMap.Services.Implementations
{
    public class AlertLifecycleRuntimeSettings
    {
        public AlertLifecycleRuntimeSettings(IConfiguration configuration)
        {
            NeedsMoreInfoAutoHideEnabled = configuration.GetValue<bool?>("AlertLifecycle:NeedsMoreInfoAutoHideEnabled") ?? true;
        }

        public bool NeedsMoreInfoAutoHideEnabled { get; private set; }

        public void SetNeedsMoreInfoAutoHideEnabled(bool enabled)
        {
            NeedsMoreInfoAutoHideEnabled = enabled;
        }
    }
}
