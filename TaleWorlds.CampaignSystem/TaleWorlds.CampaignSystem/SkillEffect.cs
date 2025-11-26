using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public sealed class SkillEffect : PropertyObject
{
	private float Bonus;

	private float BaseValue;

	private float LimitMin;

	private float LimitMax;

	public static MBReadOnlyList<SkillEffect> All => Campaign.Current.AllSkillEffects;

	public PartyRole Role { get; private set; }

	public EffectIncrementType IncrementType { get; private set; }

	public SkillObject EffectedSkill { get; private set; }

	public SkillEffect(string stringId)
		: base(stringId)
	{
	}

	public void Initialize(TextObject description, SkillObject effectedSkill, PartyRole role, float bonus, EffectIncrementType incrementType, float baseValue = 0f, float limitMin = float.MinValue, float limitMax = float.MaxValue)
	{
		Initialize(TextObject.GetEmpty(), description);
		Role = role;
		Bonus = bonus;
		IncrementType = incrementType;
		EffectedSkill = effectedSkill;
		BaseValue = baseValue;
		LimitMin = limitMin;
		LimitMax = limitMax;
		AfterInitialized();
	}

	public float GetSkillEffectValue(int skillLevel)
	{
		return MathF.Clamp(BaseValue + Bonus * (float)skillLevel, LimitMin, LimitMax);
	}
}
