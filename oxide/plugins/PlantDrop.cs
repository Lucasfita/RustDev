using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Plant Drop", "Travis Butts", 1.0)]
    [Description("Allows planting crops anywhere by dropping the seed")]
    public class PlantDrop : RustPlugin
    {
        void OnItemDropped(Item item, BaseEntity entity)
        {
            string itemName = entity.GetComponent<DroppedItem>().item.info.displayName.english;
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null) return;

            Vector3 playerPos = player.transform.position;

            switch (itemName)
            {
                case "Corn Seed":
                    CreatePlant(entity,"assets/prefabs/plants/corn/corn.entity.prefab", playerPos);
                    break;

                case "Hemp Seed":
                    CreatePlant(entity,"assets/prefabs/plants/hemp/hemp.entity.prefab", playerPos);
                    break;

                case "Pumpkin Seed":
                    CreatePlant(entity,"assets/prefabs/plants/pumpkin/pumpkin.entity.prefab", playerPos);
                    break;
            }
        }

        void CreatePlant(BaseEntity Seed,string prefab,Vector3 Pos)
        {
            PlantEntity plant = GameManager.server.CreateEntity(prefab, Pos, Quaternion.identity) as PlantEntity;

            if (plant != null)
            {
                plant.Spawn();
                Seed.Kill();
            }
        }
    }
}
