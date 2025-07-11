# Assets

# Developer guides

## Adding new pickable resource procedure

1. Add InventoryItem_StaticData (component) in "SetupStaticInventoryItemData.cs"
2. Add PickableResource_StaticData (component) in "SetupStaticResourceData.cs"
3. In the inspector add the PickableResource prefab (prefab should have ghostAuthComp, interactableResource layer and collider) along with its ID in the GameSetupAuthoring script attached to the GameSetup GM
4. When placing the pickable resource gameobject make sure to add the ResourceGameObject script and name it ResourceTypeId_`<ID>`
5. Inside the InventoryManager GM add the sprites for the equivalent output items that you get from the pickable resource (check setupStaticResourceData.cs' output items for the ResourceTypeId)
6. Delete Resource table, so a fresh one will be created with the newly added pickable resource

## Understanding the Rukhanka animation system
By adding the rukhanka Rig Definition Authoring scripts the following components are added:
    -   AnimationToProcessComponent buffer
    -   AnimationControllerLayerComponent buffer
    -   RootMotionAnimationStateComponent buffer
    -   RootMotionVelocityComponent
    -   AnimatorEntityRef
    -   RigDefinitionComponent

### Responsibilities
    -   AnimatorControllerLayerComponent:
        "The animator controller system processes entities with the AnimatorControllerLayerComponent component array. Each element in this array represents separate animation layer as specified in Unity Animator."

    -   AnimationToProcessComponent
        "During state machine processing, all prepared animations will be arranged in form of an array of AnimationToProcessComponent components."
    
    -   RigDefinitionComponent
        "Samples animations at specified times and blends results according to required blend rules. The animated entity is defined as RigDefinitionComponent"


To make it networkable the following should manually be added:
    - AnimatorControllerParameterComponent buffer
    - AnimatorControllerParameterIndexTableComponent
