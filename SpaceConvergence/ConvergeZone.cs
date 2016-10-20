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
        Vector2 basePos;
        Vector2 slotOffset;
        public Rectangle bounds;
        public bool inPlay;

        public ConvergeZone(JSONTable template, ConvergePlayer owner, ConvergeZoneId zoneId)
        {
            this.owner = owner;
            this.zoneId = zoneId;
            this.basePos = template.getVector2("basePos");
            this.slotOffset = template.getVector2("slotOffset");
            this.inPlay = template.getBool("inPlay");
            Vector2 topLeft = template.getVector2("topLeft");
            Vector2 bottomRight = template.getVector2("bottomRight");
            bounds = new Rectangle(topLeft.ToPoint(), (bottomRight - topLeft).ToPoint());
        }

        public void Add(ConvergeObject newObj)
        {
            if (newObj.zone != null)
                newObj.zone.Remove(newObj);

            newObj.slot = contents.Count;
            newObj.zone = this;
            contents.Add(newObj);
            if (!inPlay)
                newObj.ClearOnLeavingPlay();
            RenumberAll();
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

        public Vector2 GetNominalPos(int slot)
        {
            return basePos + slotOffset * slot;// + (slot%2==0?new Vector2(10,0):new Vector2(-10,0));
        }

        public void BeginTurn()
        {
            foreach (ConvergeObject obj in contents)
            {
                obj.BeginTurn();
            }
        }

        public void EndTurn()
        {
            List<ConvergeObject> deaders = new List<ConvergeObject>();
            foreach (ConvergeObject obj in contents)
            {
                obj.EndTurn();
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
