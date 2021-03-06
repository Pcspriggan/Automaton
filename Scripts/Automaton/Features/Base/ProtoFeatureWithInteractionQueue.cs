﻿namespace CryoFall.Automaton.Features
{
    using System.Collections.Generic;
    using System.Linq;
    using AtomicTorch.CBND.CoreMod.Systems.InteractionChecker;
    using AtomicTorch.CBND.GameApi.Data.World;

    public abstract class ProtoFeatureWithInteractionQueue<T> : ProtoFeature<T>
        where T : class
    {
        protected List<IStaticWorldObject> interactionQueue = new List<IStaticWorldObject>();

        /// <summary>
        /// Called by client component every tick.
        /// </summary>
        public override void Update(double deltaTime)
        {
            if (!(IsEnabled && CheckPrecondition()))
            {
                return;
            }
            CheckInteractionQueue();
        }

        protected abstract void CheckInteractionQueue();

        /// <summary>
        /// Called by client component on specific time interval.
        /// </summary>
        public override void Execute()
        {
            if (!(IsEnabled && CheckPrecondition()))
            {
                return;
            }
            FillInteractionQueue();
        }

        protected virtual void FillInteractionQueue()
        {
            using var objectsInCharacterInteractionArea = InteractionCheckerSystem
                .SharedGetTempObjectsInCharacterInteractionArea(this.CurrentCharacter);
            if (objectsInCharacterInteractionArea == null)
            {
                return;
            }
            var objectOfInterest = objectsInCharacterInteractionArea.AsList()
                                                                    .Where(t => this.EnabledEntityList.Contains(t.PhysicsBody?.AssociatedWorldObject?.ProtoGameObject))
                                                                    .ToList();
            if (!(objectOfInterest.Count > 0))
            {
                return;
            }
            foreach (var obj in objectOfInterest)
            {
                var testObject = obj.PhysicsBody.AssociatedWorldObject as IStaticWorldObject;
                if (this.TestObject(testObject))
                {
                    if (!this.interactionQueue.Contains(testObject))
                    {
                        this.interactionQueue.Add(testObject);
                    }
                }
            }
        }

        protected virtual bool TestObject(IStaticWorldObject staticWorldObject)
        {
            return staticWorldObject.ProtoStaticWorldObject
                .SharedCanInteract(CurrentCharacter, staticWorldObject, false);
        }

        /// <summary>
        /// Stop everything.
        /// </summary>
        public override void Stop()
        {
            if (interactionQueue?.Count > 0)
            {
                interactionQueue.Clear();
                InteractionCheckerSystem.CancelCurrentInteraction(CurrentCharacter);
            }
        }
    }
}
