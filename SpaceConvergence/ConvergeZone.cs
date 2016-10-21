using LRCEngine;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceConvergence
{
    public class ConvergeZone
    {
        public ConvergePlayer owner;
        public ConvergeZoneId zoneId;
        public List<ConvergeObject> contents = new List<ConvergeObject>();
        public List<ConvergeObject> newlyAdded = new List<ConvergeObject>();
        Vector2 basePos;
        Vector2 slotOffset;
        public Rectangle bounds;
        public bool inPlay;
        public bool isHidden;

        public ConvergeZone(JSONTable template, ConvergePlayer owner, ConvergeZoneId zoneId)
        {
            this.owner = owner;
            this.zoneId = zoneId;
            this.basePos = template.getVector2("basePos");
            this.slotOffset = template.getVector2("slotOffset");
            this.inPlay = template.getBool("inPlay", false);
            this.isHidden = template.getBool("isHidden", false);
            Vector2 topLeft = template.getVector2("topLeft");
            Vector2 bottomRight = template.getVector2("bottomRight");
            bounds = new Rectangle(topLeft.ToPoint(), (bottomRight - topLeft).ToPoint());
        }

        public void Add(ConvergeObject newObj)
        {
            ConvergeZone oldZone = newObj.zone;
            if (oldZone != null)
                oldZone.Remove(newObj);

            newObj.slot = contents.Count;
            newObj.zone = this;
            contents.Add(newObj);
            newlyAdded.Add(newObj);

            RenumberAll();

            if (!inPlay && oldZone != null && oldZone.inPlay)
                newObj.OnLeavingPlay();
            else if(inPlay && (oldZone == null || !oldZone.inPlay))
                newObj.OnEnteringPlay();
        }

        public void Remove(ConvergeObject oldObj)
        {
            contents.Remove(oldObj);
            oldObj.zone = null;
            oldObj.slot = 0;
            RenumberAll();
        }

        void RenumberAll()
        {
            for (int Idx = 0; Idx < contents.Count; ++Idx)
            {
                contents[Idx].slot = Idx;
            }
        }

        public void Shuffle()
        {
            for(int Idx = contents.Count - 1; Idx > 0; --Idx)
            {
                int insertPoint = Game1.rand.Next(Idx + 1);
                ConvergeObject temp = contents[Idx];
                contents[Idx] = contents[insertPoint];
                contents[insertPoint] = temp;
            }
        }

        public Vector2 GetNominalPos(int slot)
        {
            return basePos + slotOffset * slot;// + (slot%2==0?new Vector2(10,0):new Vector2(-10,0));
        }

        public void BeginMyTurn()
        {
            foreach (ConvergeObject obj in contents)
            {
                obj.BeginMyTurn();
            }
        }

        public void EndMyTurn()
        {
            List<ConvergeObject> deaders = new List<ConvergeObject>();
            foreach (ConvergeObject obj in contents)
            {
                obj.EndMyTurn();
                if (obj.dead)
                    deaders.Add(obj);
            }

            foreach (ConvergeObject deadObj in deaders)
            {
                owner.discardPile.Add(deadObj);
            }
        }
    }
}
