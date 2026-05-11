using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data;
using ThaddeusMorrowind.Bot.Features.Inventory.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Inventory;

public sealed class Phase2AdvancedService : IPhase2AdvancedService
{
    private const uint MaxCharacterLevel = 100;
    private readonly GameDbContext _dbContext;
    public Phase2AdvancedService(GameDbContext dbContext) => _dbContext = dbContext;

    public Task<Phase2Result<CharacterXpDto>> AddCharacterXpAsync(ulong actorDiscordUserId, ulong? characterId, string? characterName, long amount, bool allowAnyOwner, CancellationToken cancellationToken = default)
        => ChangeCharacterXpAsync(actorDiscordUserId, characterId, characterName, Math.Abs(amount), allowAnyOwner, cancellationToken);

    public Task<Phase2Result<CharacterXpDto>> RemoveCharacterXpAsync(ulong actorDiscordUserId, ulong? characterId, string? characterName, long amount, bool allowAnyOwner, CancellationToken cancellationToken = default)
        => ChangeCharacterXpAsync(actorDiscordUserId, characterId, characterName, -Math.Abs(amount), allowAnyOwner, cancellationToken);

    private async Task<Phase2Result<CharacterXpDto>> ChangeCharacterXpAsync(ulong actorDiscordUserId, ulong? characterId, string? characterName, long amount, bool allowAnyOwner, CancellationToken ct)
    {
        if (amount == 0) return Phase2Result<CharacterXpDto>.Fail("La cantidad de experiencia no puede ser 0.");
        DbConnection c = await OpenAsync(ct);
        CharacterRef? ch = await ResolveCharacterAsync(c, actorDiscordUserId, characterId, characterName, allowAnyOwner, ct);
        if (ch is null) return Phase2Result<CharacterXpDto>.Fail("No encontré ese personaje.");
        ulong newTotal = amount > 0 ? ch.TotalXp + (ulong)amount : ch.TotalXp > (ulong)Math.Abs(amount) ? ch.TotalXp - (ulong)Math.Abs(amount) : 0;
        (uint level, ulong current, ulong next) = CalcLevel(newTotal);
        await using (DbCommand cmd = c.CreateCommand())
        {
            cmd.CommandText = "UPDATE characters SET level=@level,current_xp=@current,total_xp=@total,updated_at=CURRENT_TIMESTAMP WHERE id=@id;";
            Add(cmd,"@level",level); Add(cmd,"@current",current); Add(cmd,"@total",newTotal); Add(cmd,"@id",ch.Id);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await RecalcStatsAsync(c, ch.Id, ct);
        return Phase2Result<CharacterXpDto>.Ok(new CharacterXpDto(ch.Id, ch.Name, level, current, newTotal, next), amount > 0 ? "Experiencia agregada." : "Experiencia retirada.");
    }

    private static (uint level, ulong current, ulong next) CalcLevel(ulong total)
    {
        ulong remaining = total; uint level = 1;
        while (level < MaxCharacterLevel)
        {
            ulong need = (ulong)Math.Round(100 * Math.Pow(level, 1.8));
            if (remaining < need) break;
            remaining -= need; level++;
        }
        return level >= MaxCharacterLevel ? (MaxCharacterLevel, 0, 0) : (level, remaining, (ulong)Math.Round(100 * Math.Pow(level, 1.8)) - remaining);
    }

    public async Task<IReadOnlyList<ArtifactSetDto>> ListArtifactSetsAsync(string? filter, CancellationToken cancellationToken = default)
    {
        DbConnection c = await OpenAsync(cancellationToken);
        List<ArtifactSetDto> sets = new();
        await using DbCommand cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT set_key,set_type,source_key,name,description,two_piece_summary,four_piece_summary FROM artifact_sets WHERE is_active=TRUE AND (@f IS NULL OR set_key LIKE CONCAT('%',@f,'%') OR name LIKE CONCAT('%',@f,'%') OR set_type LIKE CONCAT('%',@f,'%') OR source_key LIKE CONCAT('%',@f,'%')) ORDER BY display_order,name LIMIT 40;";
        Add(cmd,"@f",string.IsNullOrWhiteSpace(filter)?null:filter.Trim());
        await using DbDataReader r = await cmd.ExecuteReaderAsync(cancellationToken);
        while(await r.ReadAsync(cancellationToken))
        {
            string key = S(r,"set_key");
            sets.Add(new ArtifactSetDto(key,S(r,"set_type"),N(r,"source_key"),S(r,"name"),N(r,"description"),N(r,"two_piece_summary"),N(r,"four_piece_summary"),Array.Empty<string>()));
        }
        List<ArtifactSetDto> final = new();
        foreach(var set in sets) final.Add(set with { Bonuses = await SetBonusesAsync(c,set.SetKey,cancellationToken) });
        return final;
    }

    public async Task<ArtifactSetDto?> GetArtifactSetAsync(string setKey, CancellationToken cancellationToken = default)
        => (await ListArtifactSetsAsync(setKey, cancellationToken)).FirstOrDefault(x=>x.SetKey.Equals(setKey,StringComparison.OrdinalIgnoreCase)) ?? (await ListArtifactSetsAsync(setKey, cancellationToken)).FirstOrDefault();

    private async Task<IReadOnlyList<string>> SetBonusesAsync(DbConnection c, string setKey, CancellationToken ct)
    {
        List<string> list = new();
        await using DbCommand cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT b.pieces_required,b.bonus_name,b.bonus_description FROM artifact_set_bonuses b INNER JOIN artifact_sets s ON s.id=b.artifact_set_id WHERE s.set_key=@key ORDER BY b.pieces_required;";
        Add(cmd,"@key",setKey);
        await using DbDataReader r = await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) list.Add($"{I(r,"pieces_required")}p · {S(r,"bonus_name")}: {S(r,"bonus_description")}");
        return list;
    }

    public async Task<IReadOnlyList<WeaponDto>> ListWeaponsAsync(string? filter, CancellationToken cancellationToken = default)
    {
        DbConnection c = await OpenAsync(cancellationToken); List<WeaponDto> list = new();
        await using DbCommand cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT t.id,t.item_key,t.name,t.description,rar.name rarity_name,rar.stars,main.name main_stat,sec.name secondary_stat FROM item_templates t INNER JOIN item_categories cat ON cat.id=t.category_id LEFT JOIN item_rarities rar ON rar.id=t.rarity_id LEFT JOIN weapon_template_details wd ON wd.item_template_id=t.id LEFT JOIN stat_types main ON main.id=wd.main_stat_type_id LEFT JOIN stat_types sec ON sec.id=wd.secondary_stat_type_id WHERE cat.category_key='arma' AND t.is_active=TRUE AND (@f IS NULL OR t.item_key LIKE CONCAT('%',@f,'%') OR t.name LIKE CONCAT('%',@f,'%')) ORDER BY rar.stars DESC,t.display_order LIMIT 40;";
        Add(cmd,"@f",string.IsNullOrWhiteSpace(filter)?null:filter.Trim());
        await using DbDataReader r = await cmd.ExecuteReaderAsync(cancellationToken);
        while(await r.ReadAsync(cancellationToken)) list.Add(new WeaponDto(S(r,"item_key"),S(r,"name"),N(r,"description"),N(r,"rarity_name"),r["stars"] is DBNull?null:I(r,"stars"),N(r,"main_stat"),N(r,"secondary_stat"),Array.Empty<string>()));
        List<WeaponDto> final = new(); foreach(var w in list) final.Add(w with { Passives = await WeaponPassivesAsync(c,w.ItemKey,cancellationToken) }); return final;
    }

    public async Task<WeaponDto?> GetWeaponAsync(string itemKey, CancellationToken cancellationToken = default)
        => (await ListWeaponsAsync(itemKey,cancellationToken)).FirstOrDefault(x=>x.ItemKey.Equals(itemKey,StringComparison.OrdinalIgnoreCase)) ?? (await ListWeaponsAsync(itemKey,cancellationToken)).FirstOrDefault();

    private async Task<IReadOnlyList<string>> WeaponPassivesAsync(DbConnection c, string itemKey, CancellationToken ct)
    {
        List<string> p = new(); await using DbCommand cmd = c.CreateCommand();
        cmd.CommandText=@"SELECT wtp.passive_slot,wpt.name,wpt.passive_type,wpt.description,wpt.min_stars FROM weapon_template_passives wtp INNER JOIN weapon_passive_templates wpt ON wpt.id=wtp.passive_template_id INNER JOIN item_templates t ON t.id=wtp.item_template_id WHERE t.item_key=@key ORDER BY wpt.display_order;";
        Add(cmd,"@key",itemKey); await using DbDataReader r = await cmd.ExecuteReaderAsync(ct); while(await r.ReadAsync(ct)) p.Add($"{S(r,"passive_slot")} · {S(r,"name")} ({I(r,"min_stars")}★): {S(r,"description")}"); return p;
    }

    public async Task<IReadOnlyList<ItemTemplateSearchDto>> SearchItemsAsync(string? term, string? categoryKey, CancellationToken cancellationToken = default)
    {
        DbConnection c = await OpenAsync(cancellationToken); List<ItemTemplateSearchDto> items = new();
        await using DbCommand cmd = c.CreateCommand();
        cmd.CommandText=@"SELECT t.item_key,t.name,cat.category_key,t.item_subtype,rar.name rarity_name,rar.stars,t.description FROM item_templates t INNER JOIN item_categories cat ON cat.id=t.category_id LEFT JOIN item_rarities rar ON rar.id=t.rarity_id WHERE t.is_active=TRUE AND (@cat IS NULL OR cat.category_key=@cat) AND (@term IS NULL OR t.item_key LIKE CONCAT('%',@term,'%') OR t.name LIKE CONCAT('%',@term,'%') OR t.description LIKE CONCAT('%',@term,'%')) ORDER BY cat.display_order,t.display_order LIMIT 40;";
        Add(cmd,"@cat",string.IsNullOrWhiteSpace(categoryKey)?null:categoryKey.Trim()); Add(cmd,"@term",string.IsNullOrWhiteSpace(term)?null:term.Trim());
        await using DbDataReader r = await cmd.ExecuteReaderAsync(cancellationToken); while(await r.ReadAsync(cancellationToken)) items.Add(new ItemTemplateSearchDto(S(r,"item_key"),S(r,"name"),S(r,"category_key"),N(r,"item_subtype"),N(r,"rarity_name"),r["stars"] is DBNull?null:I(r,"stars"),N(r,"description"))); return items;
    }

    public Task<Phase2Result<ArtifactXpDto>> AddArtifactXpAsync(ulong discordUserId, ulong itemInstanceId, long amount, CancellationToken cancellationToken = default) => ChangeArtifactXpAsync(discordUserId,itemInstanceId,Math.Abs(amount),cancellationToken);
    public Task<Phase2Result<ArtifactXpDto>> RemoveArtifactXpAsync(ulong discordUserId, ulong itemInstanceId, long amount, CancellationToken cancellationToken = default) => ChangeArtifactXpAsync(discordUserId,itemInstanceId,-Math.Abs(amount),cancellationToken);

    private async Task<Phase2Result<ArtifactXpDto>> ChangeArtifactXpAsync(ulong discordUserId, ulong itemId, long delta, CancellationToken ct)
    {
        if(delta==0) return Phase2Result<ArtifactXpDto>.Fail("La experiencia no puede ser 0.");
        DbConnection c = await OpenAsync(ct); ArtifactRef? a = await LoadArtifactAsync(c,discordUserId,itemId,ct); if(a is null) return Phase2Result<ArtifactXpDto>.Fail("No encontré ese artefacto/herramienta única en tu inventario.");
        if(!a.Subtype.StartsWith("artifact_",StringComparison.OrdinalIgnoreCase) && a.Subtype!="unique_tool") return Phase2Result<ArtifactXpDto>.Fail("Ese item no es artefacto ni herramienta única.");
        ulong newXp = delta>0 ? a.Xp+(ulong)delta : a.Xp>(ulong)Math.Abs(delta)?a.Xp-(ulong)Math.Abs(delta):0;
        int newLevel = await ArtifactLevelAsync(c,a.RarityId,newXp,a.MaxLevel,ct); List<string> events = new(); await EnsureArtifactAsync(c,a,events,ct);
        if(newLevel>a.Level){ for(int lvl=a.Level+1; lvl<=newLevel; lvl++) if(lvl%4==0) await ArtifactMilestoneAsync(c,a,lvl,events,ct); }
        else if(newLevel<a.Level) events.Add("Debug: bajó XP/nivel, pero no se revierten substats para no corromper historial.");
        await using DbCommand cmd=c.CreateCommand(); cmd.CommandText="UPDATE item_instances SET experience=@xp, level=@lvl, updated_at=CURRENT_TIMESTAMP WHERE id=@id;"; Add(cmd,"@xp",newXp); Add(cmd,"@lvl",newLevel); Add(cmd,"@id",itemId); await cmd.ExecuteNonQueryAsync(ct);
        return Phase2Result<ArtifactXpDto>.Ok(new ArtifactXpDto(itemId,a.Name,a.Level,newLevel,a.Xp,newXp,a.MaxLevel,events),"Experiencia de artefacto actualizada.");
    }

    public async Task<FinalStatsDto?> GetFinalStatsAsync(ulong discordUserId, ulong? characterId, string? characterName, CancellationToken cancellationToken = default)
    {
        DbConnection c = await OpenAsync(cancellationToken); CharacterRef? ch = await ResolveCharacterAsync(c,discordUserId,characterId,characterName,false,cancellationToken); if(ch is null) return null;
        Dictionary<string,Acc> stats = await BaseStatsAsync(c,ch.Id,cancellationToken); await EquipmentModsAsync(c,ch.Id,stats,cancellationToken); await SetModsAsync(c,ch.Id,stats,cancellationToken);
        return new FinalStatsDto(ch.Id,ch.Name,stats.Values.OrderBy(x=>x.Order).Select(x=>{ decimal before=x.Base+x.Flat; return new FinalStatDto(x.Key,x.Name,x.Kind,x.Base,x.Flat,x.Percent,Math.Round(before+(before*x.Percent/100m),2)); }).ToList());
    }

    // ---------- Helpers de artefactos ----------
    private async Task<ArtifactRef?> LoadArtifactAsync(DbConnection c, ulong discordId, ulong itemId, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=@"SELECT ii.id,ii.experience,ii.level,t.item_key,t.name,t.item_subtype,rar.id rarity_id,rar.stars,rar.max_level FROM item_instances ii JOIN user_accounts u ON u.id=ii.owner_user_account_id JOIN item_templates t ON t.id=ii.item_template_id LEFT JOIN item_rarities rar ON rar.id=t.rarity_id WHERE ii.id=@id AND u.discord_user_id=@du LIMIT 1;"; Add(cmd,"@id",itemId); Add(cmd,"@du",discordId); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct)) return null; return new ArtifactRef(U(r,"id"),S(r,"item_key"),S(r,"name"),S(r,"item_subtype"),U(r,"rarity_id"),I(r,"stars"),I(r,"max_level"),U(r,"experience"),I(r,"level")); }
    private async Task<int> ArtifactLevelAsync(DbConnection c, ulong rarityId, ulong xp, int max, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText="SELECT COALESCE(MAX(target_level),0) FROM artifact_level_rules WHERE rarity_id=@r AND target_level<=@m AND cumulative_xp_required<=@xp;"; Add(cmd,"@r",rarityId);Add(cmd,"@m",max);Add(cmd,"@xp",xp); return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)); }
    private async Task EnsureArtifactAsync(DbConnection c, ArtifactRef a, List<string> events, CancellationToken ct){ await using(DbCommand chk=c.CreateCommand()){chk.CommandText="SELECT COUNT(*) FROM artifact_instance_details WHERE item_instance_id=@id;";Add(chk,"@id",a.Id); if(Convert.ToUInt64(await chk.ExecuteScalarAsync(ct))>0) return;} ulong? slot=await ScalarU(c,"SELECT id FROM equipment_slots WHERE slot_key=@v LIMIT 1;",("@v",a.Subtype),ct); if(slot is null){events.Add("Sin slot válido para inicializar."); return;} MainRef? main=await MainRuleAsync(c,slot.Value,ct); if(main is null){events.Add("Sin main stat válida."); return;} ulong? set=await GuessSetAsync(c,a.ItemKey,ct); await using(DbCommand cmd=c.CreateCommand()){cmd.CommandText=@"INSERT INTO artifact_instance_details(item_instance_id,artifact_slot_id,artifact_set_id,main_stat_type_id,main_modifier_kind,main_stat_value) VALUES(@id,@slot,@set,@stat,@kind,@val);"; Add(cmd,"@id",a.Id);Add(cmd,"@slot",slot.Value);Add(cmd,"@set",set);Add(cmd,"@stat",main.StatId);Add(cmd,"@kind",main.Kind);Add(cmd,"@val",main.Kind=="percent"?4*a.Stars:25*a.Stars); await cmd.ExecuteNonQueryAsync(ct);} events.Add($"Artefacto inicializado: main {main.Name} ({main.Kind})."); int initial=a.Stars<=1?0:a.Stars==2?1:a.Stars==3?2:3; for(int i=0;i<initial;i++) await AddSubAsync(c,a,main,false,events,ct); }
    private async Task ArtifactMilestoneAsync(DbConnection c, ArtifactRef a, int level, List<string> events, CancellationToken ct){ int count=await ScalarI(c,"SELECT COUNT(*) FROM artifact_instance_substats WHERE item_instance_id=@id;",("@id",a.Id),ct); MainRef? main=await LoadMainAsync(c,a.Id,ct); if(main is null){events.Add($"+{level}: sin main stat.");return;} if(count<4){await AddSubAsync(c,a,main,true,events,ct);events.Add($"+{level}: substat nueva añadida y mejorada una vez.");} else {await ImproveSubAsync(c,a,events,ct);events.Add($"+{level}: substat existente mejorada.");} }
    private async Task<MainRef?> MainRuleAsync(DbConnection c, ulong slotId, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=@"SELECT r.stat_type_id,r.modifier_kind,s.name FROM artifact_main_stat_rules r JOIN stat_types s ON s.id=r.stat_type_id WHERE r.slot_id=@slot AND r.is_active=TRUE ORDER BY s.display_order LIMIT 1;"; Add(cmd,"@slot",slotId); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct))return null; return new MainRef(U(r,"stat_type_id"),S(r,"name"),S(r,"modifier_kind")); }
    private async Task<MainRef?> LoadMainAsync(DbConnection c, ulong itemId, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=@"SELECT d.main_stat_type_id,d.main_modifier_kind,s.name FROM artifact_instance_details d JOIN stat_types s ON s.id=d.main_stat_type_id WHERE d.item_instance_id=@id LIMIT 1;"; Add(cmd,"@id",itemId); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct))return null; return new MainRef(U(r,"main_stat_type_id"),S(r,"name"),S(r,"main_modifier_kind")); }
    private async Task AddSubAsync(DbConnection c, ArtifactRef a, MainRef main, bool improveOnce, List<string> events, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=@"SELECT r.stat_type_id,r.modifier_kind,s.name,COALESCE((rv.roll_min+rv.roll_max)/2,1) rollv FROM artifact_substat_rules r JOIN stat_types s ON s.id=r.stat_type_id LEFT JOIN artifact_stat_roll_values rv ON rv.stat_type_id=r.stat_type_id AND rv.modifier_kind=r.modifier_kind AND rv.rarity_id=@rar WHERE r.is_active=TRUE AND NOT(r.stat_type_id=@main AND r.modifier_kind=@kind) AND NOT EXISTS(SELECT 1 FROM artifact_instance_substats ex WHERE ex.item_instance_id=@id AND ex.stat_type_id=r.stat_type_id AND ex.modifier_kind=r.modifier_kind) ORDER BY s.display_order,r.modifier_kind LIMIT 1;"; Add(cmd,"@rar",a.RarityId);Add(cmd,"@main",main.StatId);Add(cmd,"@kind",main.Kind);Add(cmd,"@id",a.Id); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct)){events.Add("No hay substat disponible.");return;} ulong stat=U(r,"stat_type_id"); string kind=S(r,"modifier_kind"), name=S(r,"name"); decimal roll=Convert.ToDecimal(r["rollv"]); await r.DisposeAsync(); int rolls=improveOnce?2:1; decimal val=roll*rolls; await using DbCommand ins=c.CreateCommand(); ins.CommandText="INSERT INTO artifact_instance_substats(item_instance_id,stat_type_id,modifier_kind,value,roll_count,display_order) VALUES(@id,@stat,@kind,@val,@rolls,999);"; Add(ins,"@id",a.Id);Add(ins,"@stat",stat);Add(ins,"@kind",kind);Add(ins,"@val",val);Add(ins,"@rolls",rolls); await ins.ExecuteNonQueryAsync(ct); events.Add($"Substat agregada: {name} {(kind=="percent"?$"{val:0.##}%":$"{val:0.##}")}."); }
    private async Task ImproveSubAsync(DbConnection c, ArtifactRef a, List<string> events, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=@"SELECT sub.id,sub.modifier_kind,s.name,COALESCE((rv.roll_min+rv.roll_max)/2,1) rollv FROM artifact_instance_substats sub JOIN stat_types s ON s.id=sub.stat_type_id LEFT JOIN artifact_stat_roll_values rv ON rv.stat_type_id=sub.stat_type_id AND rv.modifier_kind=sub.modifier_kind AND rv.rarity_id=@rar WHERE sub.item_instance_id=@id ORDER BY sub.roll_count,sub.id LIMIT 1;"; Add(cmd,"@rar",a.RarityId);Add(cmd,"@id",a.Id); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct)){events.Add("No hay substat para mejorar.");return;} ulong id=U(r,"id"); string name=S(r,"name"), kind=S(r,"modifier_kind"); decimal roll=Convert.ToDecimal(r["rollv"]); await r.DisposeAsync(); await using DbCommand up=c.CreateCommand(); up.CommandText="UPDATE artifact_instance_substats SET value=value+@v, roll_count=roll_count+1 WHERE id=@id;"; Add(up,"@v",roll);Add(up,"@id",id); await up.ExecuteNonQueryAsync(ct); events.Add($"Substat mejorada: {name} +{(kind=="percent"?$"{roll:0.##}%":$"{roll:0.##}")}."); }
    private async Task<ulong?> GuessSetAsync(DbConnection c, string itemKey, CancellationToken ct){ string key=itemKey.Contains("corte_lunar",StringComparison.OrdinalIgnoreCase)?"corte_lunar": itemKey.Contains("redencion",StringComparison.OrdinalIgnoreCase)?"alas_alba_sagrada":"sello_juicio_arcano"; return await ScalarU(c,"SELECT id FROM artifact_sets WHERE set_key=@v LIMIT 1;",("@v",key),ct); }

    // ---------- Stats finales ----------
    private async Task<Dictionary<string,Acc>> BaseStatsAsync(DbConnection c, ulong charId, CancellationToken ct){ Dictionary<string,Acc> d=new(); await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=@"SELECT s.stat_key,s.name,s.value_kind,s.display_order,COALESCE(cs.base_value,s.default_base) base_value,COALESCE(cs.extra_value,0) extra_value FROM stat_types s LEFT JOIN character_stats cs ON cs.stat_type_id=s.id AND cs.character_id=@id WHERE s.is_active=TRUE ORDER BY s.display_order;"; Add(cmd,"@id",charId); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct); while(await r.ReadAsync(ct)){ string k=S(r,"stat_key"); d[k]=new Acc(k,S(r,"name"),S(r,"value_kind"),I(r,"display_order"),Convert.ToDecimal(r["base_value"]),Convert.ToDecimal(r["extra_value"]),0); } return d; }
    private async Task EquipmentModsAsync(DbConnection c, ulong charId, Dictionary<string,Acc> d, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=@"SELECT s.stat_key,m.modifier_kind,m.value FROM character_equipment_slots e JOIN item_instances ii ON ii.id=e.item_instance_id JOIN item_templates t ON t.id=ii.item_template_id JOIN item_stat_modifiers m ON m.item_template_id=t.id JOIN stat_types s ON s.id=m.stat_type_id WHERE e.character_id=@id UNION ALL SELECT s.stat_key,m.modifier_kind,m.value FROM character_equipment_slots e JOIN item_stat_modifiers m ON m.item_instance_id=e.item_instance_id JOIN stat_types s ON s.id=m.stat_type_id WHERE e.character_id=@id UNION ALL SELECT s.stat_key,d.main_modifier_kind,d.main_stat_value FROM character_equipment_slots e JOIN artifact_instance_details d ON d.item_instance_id=e.item_instance_id JOIN stat_types s ON s.id=d.main_stat_type_id WHERE e.character_id=@id UNION ALL SELECT s.stat_key,sub.modifier_kind,sub.value FROM character_equipment_slots e JOIN artifact_instance_substats sub ON sub.item_instance_id=e.item_instance_id JOIN stat_types s ON s.id=sub.stat_type_id WHERE e.character_id=@id;"; Add(cmd,"@id",charId); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct); while(await r.ReadAsync(ct)) Apply(d,S(r,"stat_key"),S(r,"modifier_kind"),Convert.ToDecimal(r["value"])); }
    private async Task SetModsAsync(DbConnection c, ulong charId, Dictionary<string,Acc> d, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=@"SELECT s.stat_key,m.modifier_kind,m.value FROM (SELECT ad.artifact_set_id,COUNT(*) pieces FROM character_equipment_slots e JOIN equipment_slots sl ON sl.id=e.slot_id AND sl.counts_for_artifact_set=TRUE JOIN artifact_instance_details ad ON ad.item_instance_id=e.item_instance_id WHERE e.character_id=@id AND ad.artifact_set_id IS NOT NULL GROUP BY ad.artifact_set_id) active JOIN set_bonus_stat_modifiers m ON m.artifact_set_id=active.artifact_set_id AND m.pieces_required<=active.pieces JOIN stat_types s ON s.id=m.stat_type_id;"; Add(cmd,"@id",charId); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct); while(await r.ReadAsync(ct)) Apply(d,S(r,"stat_key"),S(r,"modifier_kind"),Convert.ToDecimal(r["value"])); }
    private static void Apply(Dictionary<string,Acc>d,string key,string kind,decimal val){ if(!d.TryGetValue(key,out var a)) return; d[key]=kind.Equals("percent",StringComparison.OrdinalIgnoreCase)?a with{Percent=a.Percent+val}:a with{Flat=a.Flat+val}; }

    // ---------- DB helpers ----------
    private async Task RecalcStatsAsync(DbConnection c, ulong characterId, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=@"UPDATE character_stats cs JOIN characters ch ON ch.id=cs.character_id JOIN stat_types s ON s.id=cs.stat_type_id JOIN nations n ON n.id=ch.nation_id JOIN roles r ON r.id=ch.role_id JOIN professions p ON p.id=ch.profession_id SET cs.base_value=ROUND(s.default_base+((ch.level-1)*COALESCE((SELECT SUM(g.growth_per_level) FROM stat_growth_rules g WHERE g.stat_type_id=s.id AND g.is_active=TRUE AND ((g.source_type='base' AND g.source_key='global') OR (g.source_type='nation' AND g.source_key=n.nation_key) OR (g.source_type='role' AND g.source_key=r.role_key) OR (g.source_type='profession' AND g.source_key=p.profession_key))),0))+COALESCE((SELECT SUM(g.flat_bonus) FROM stat_growth_rules g WHERE g.stat_type_id=s.id AND g.is_active=TRUE AND ((g.source_type='base' AND g.source_key='global') OR (g.source_type='nation' AND g.source_key=n.nation_key) OR (g.source_type='role' AND g.source_key=r.role_key) OR (g.source_type='profession' AND g.source_key=p.profession_key))),0),2), cs.updated_at=CURRENT_TIMESTAMP WHERE ch.id=@id AND s.is_active=TRUE;"; Add(cmd,"@id",characterId); await cmd.ExecuteNonQueryAsync(ct); }
    private async Task<CharacterRef?> ResolveCharacterAsync(DbConnection c, ulong actor, ulong? id, string? name, bool any, CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); if(id is not null && id>0){cmd.CommandText=@"SELECT ch.id,ch.name,ch.current_xp,ch.total_xp FROM characters ch JOIN user_accounts u ON u.id=ch.user_account_id WHERE ch.id=@id AND (@any=TRUE OR u.discord_user_id=@actor) LIMIT 1;"; Add(cmd,"@id",id.Value);} else {cmd.CommandText=@"SELECT ch.id,ch.name,ch.current_xp,ch.total_xp FROM characters ch JOIN user_accounts u ON u.id=ch.user_account_id WHERE (@nm IS NULL OR LOWER(TRIM(ch.name))=LOWER(TRIM(@nm))) AND (@any=TRUE OR u.discord_user_id=@actor) ORDER BY ch.id DESC LIMIT 1;"; Add(cmd,"@nm",string.IsNullOrWhiteSpace(name)?null:name.Trim());} Add(cmd,"@any",any);Add(cmd,"@actor",actor); await using DbDataReader r=await cmd.ExecuteReaderAsync(ct); if(!await r.ReadAsync(ct)) return null; return new CharacterRef(U(r,"id"),S(r,"name"),U(r,"current_xp"),U(r,"total_xp")); }
    private async Task<DbConnection> OpenAsync(CancellationToken ct){ DbConnection c=_dbContext.Database.GetDbConnection(); if(c.State!=ConnectionState.Open) await c.OpenAsync(ct); return c; }
    private static async Task<int> ScalarI(DbConnection c,string sql,(string,object?) p,CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=sql; Add(cmd,p.Item1,p.Item2); return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)); }
    private static async Task<ulong?> ScalarU(DbConnection c,string sql,(string,object?) p,CancellationToken ct){ await using DbCommand cmd=c.CreateCommand(); cmd.CommandText=sql; Add(cmd,p.Item1,p.Item2); object? o=await cmd.ExecuteScalarAsync(ct); return o is null or DBNull?null:Convert.ToUInt64(o); }
    private static void Add(DbCommand cmd,string name,object? value){ DbParameter p=cmd.CreateParameter(); p.ParameterName=name; p.Value=value??DBNull.Value; cmd.Parameters.Add(p); }
    private static string S(DbDataReader r,string n)=>Convert.ToString(r[n])??string.Empty; private static string? N(DbDataReader r,string n)=>r[n] is DBNull?null:Convert.ToString(r[n]); private static int I(DbDataReader r,string n)=>Convert.ToInt32(r[n]); private static ulong U(DbDataReader r,string n)=>Convert.ToUInt64(r[n]);
    private sealed record CharacterRef(ulong Id,string Name,ulong CurrentXp,ulong TotalXp);
    private sealed record ArtifactRef(ulong Id,string ItemKey,string Name,string Subtype,ulong RarityId,int Stars,int MaxLevel,ulong Xp,int Level);
    private sealed record MainRef(ulong StatId,string Name,string Kind);
    private sealed record Acc(string Key,string Name,string Kind,int Order,decimal Base,decimal Flat,decimal Percent);
}
