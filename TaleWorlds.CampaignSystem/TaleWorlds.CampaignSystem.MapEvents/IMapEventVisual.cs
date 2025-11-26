namespace TaleWorlds.CampaignSystem.MapEvents;

public interface IMapEventVisual
{
	void Initialize(CampaignVec2 position, int battleSizeValue, bool isVisible);

	void OnMapEventEnd();

	void SetVisibility(bool isVisible);
}
