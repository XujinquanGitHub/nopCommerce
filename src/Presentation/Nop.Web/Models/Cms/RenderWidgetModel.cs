using Nop.Web.Framework.Mvc.Models;

namespace Nop.Web.Models.Cms
{
    public partial class RenderWidgetModel : BaseNopModel
    {
        public string WidgetViewComponentName { get; set; }
        public string WidgetZone { get; set; }
        public object AdditionalData { get; set; }
    }
}