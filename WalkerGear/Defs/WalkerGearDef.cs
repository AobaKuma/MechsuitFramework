using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WalkerGear
{
    public class WalkerGearDef : Def
    {
        public float health; //血量
        public float shield;  //護盾,沒填就沒有.
        public float movespeed; //移動速度
        //顯示
        public GraphicDataWithOffset shieldGraphic;   //護盾渲染
        public GraphicDataWithOffset frontGraphic;    //上層渲染
        public GraphicDataWithOffset backGraphic; //下層渲染
        //主武器方面
        public ThingDef weapon; //默認武器，在沒有裝備其他武器時就會自動裝備
        public bool equipWeapon; //如果為真則可以裝備其他武器
        //必填
        public ThingDef core;
        public ThingDef building;
        public ThingDef wreckage;
        public ExplosionData selfExplosive;
        //其他设置
        public bool sufferEMP = false;
        public bool sufferStun = false;


    }
    public class GraphicDataWithOffset
    {
        public Vector3 offSet = Vector3.zero;
        public GraphicData graphicData;
    }
    public class ExplosionData
    {
        public int amount;
        public float radius;
        public DamageDef damageDef;
    }
}
