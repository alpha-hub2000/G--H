using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using xna = Microsoft.Xna.Framework;
using URWPGSim2D.Common;
using URWPGSim2D.StrategyLoader;

namespace URWPGSim2D.Strategy
{
    public class Strategy : MarshalByRefObject, IStrategy
    {
        #region reserved code never be changed or removed
        /// <summary>
        /// override the InitializeLifetimeService to return null instead of a valid ILease implementation
        /// to ensure this type of remote object never dies
        /// </summary>
        /// <returns>null</returns>
        public override object InitializeLifetimeService()
        {
            //return base.InitializeLifetimeService();
            return null; // makes the object live indefinitely
        }
        #endregion

        /// <summary>
        /// 决策类当前对象对应的仿真使命参与队伍的决策数组引用 第一次调用GetDecision时分配空间
        /// </summary>
        private Decision[] decisions = null;

        /// <summary>
        /// 获取队伍名称 在此处设置参赛队伍的名称
        /// </summary>
        /// <returns>队伍名称字符串</returns>
        public string GetTeamName()
        {
            return "生死时速_defence";
        }
        static int flag = 0;
        int times = 0;

        /// <summary>
        /// 获取当前仿真使命（比赛项目）当前队伍所有仿真机器鱼的决策数据构成的数组
        /// </summary>
        /// <param name="mission">服务端当前运行着的仿真使命Mission对象</param>
        /// <param name="teamId">当前队伍在服务端运行着的仿真使命中所处的编号 
        /// 用于作为索引访问Mission对象的TeamsRef队伍列表中代表当前队伍的元素</param>
        /// <returns>当前队伍所有仿真机器鱼的决策数据构成的Decision数组对象</returns>
        public Decision[] GetDecision(Mission mission, int teamId)
        {
            // 决策类当前对象第一次调用GetDecision时Decision数组引用为null
            if (decisions == null)
            {// 根据决策类当前对象对应的仿真使命参与队伍仿真机器鱼的数量分配决策数组空间
                decisions = new Decision[mission.CommonPara.FishCntPerTeam];
            }

            #region 决策计算过程 需要各参赛队伍实现的部分
            #region 策略编写帮助信息
            //====================我是华丽的分割线====================//
            //======================策略编写指南======================//
            //1.策略编写工作直接目标是给当前队伍决策数组decisions各元素填充决策值
            //2.决策数据类型包括两个int成员，VCode为速度档位值，TCode为转弯档位值
            //3.VCode取值范围0-14共15个整数值，每个整数对应一个速度值，速度值整体但非严格递增
            //有个别档位值对应的速度值低于比它小的档位值对应的速度值，速度值数据来源于实验
            //4.TCode取值范围0-14共15个整数值，每个整数对应一个角速度值
            //整数7对应直游，角速度值为0，整数6-0，8-14分别对应左转和右转，偏离7越远，角度速度值越大
            //5.任意两个速度/转弯档位之间切换，都需要若干个仿真周期，才能达到稳态速度/角速度值
            //目前运动学计算过程决定稳态速度/角速度值接近但小于目标档位对应的速度/角速度值
            //6.决策类Strategy的实例在加载完毕后一直存在于内存中，可以自定义私有成员变量保存必要信息
            //但需要注意的是，保存的信息在中途更换策略时将会丢失
            //====================我是华丽的分割线====================//
            //=======策略中可以使用的比赛环境信息和过程信息说明=======//
            //场地坐标系: 以毫米为单位，矩形场地中心为原点，向右为正X，向下为正Z
            //            负X轴顺时针转回负X轴角度范围为(-PI,PI)的坐标系，也称为世界坐标系
            //mission.CommonPara: 当前仿真使命公共参数
            //mission.CommonPara.FishCntPerTeam: 每支队伍仿真机器鱼数量
            //mission.CommonPara.MsPerCycle: 仿真周期毫秒数
            //mission.CommonPara.RemainingCycles: 当前剩余仿真周期数
            //mission.CommonPara.TeamCount: 当前仿真使命参与队伍数量
            //mission.CommonPara.TotalSeconds: 当前仿真使命运行时间秒数
            //mission.EnvRef.Balls: 
            //当前仿真使命涉及到的仿真水球列表，列表元素的成员意义参见URWPGSim2D.Common.Ball类定义中的注释
            //mission.EnvRef.FieldInfo: 
            //当前仿真使命涉及到的仿真场地，各成员意义参见URWPGSim2D.Common.Field类定义中的注释
            //mission.EnvRef.ObstaclesRect: 
            //当前仿真使命涉及到的方形障碍物列表，列表元素的成员意义参见URWPGSim2D.Common.RectangularObstacle类定义中的注释
            //mission.EnvRef.ObstaclesRound:
            //当前仿真使命涉及到的圆形障碍物列表，列表元素的成员意义参见URWPGSim2D.Common.RoundedObstacle类定义中的注释
            //mission.TeamsRef[teamId]:
            //决策类当前对象对应的仿真使命参与队伍（当前队伍）
            //mission.TeamsRef[teamId].Para:
            //当前队伍公共参数，各成员意义参见URWPGSim2D.Common.TeamCommonPara类定义中的注释
            //mission.TeamsRef[teamId].Fishes:
            //当前队伍仿真机器鱼列表，列表元素的成员意义参见URWPGSim2D.Common.RoboFish类定义中的注释
            //mission.TeamsRef[teamId].Fishes[i].PositionMm和PolygonVertices[0],BodyDirectionRad,VelocityMmPs,
            //                                   AngularVelocityRadPs,Tactic:
            //当前队伍第i条仿真机器鱼鱼体矩形中心和鱼头顶点在场地坐标系中的位置（用到X坐标和Z坐标），鱼体方向，速度值，
            //                                   角速度值，决策值
            //====================我是华丽的分割线====================//
            //========================典型循环========================//
            //for (int i = 0; i < mission.CommonPara.FishCntPerTeam; i++)
            //{
            //  decisions[i].VCode = 0; // 静止
            //  decisions[i].TCode = 7; // 直游
            //}
            //====================我是华丽的分割线====================//
            #endregion
            //请从这里开始编写代码
            #region 获得鱼和球的数组
            RoboFish[] fishes = new RoboFish[1];
            fishes[0] = mission.TeamsRef[teamId].Fishes[0];
           // fishes[1] = mission.TeamsRef[(teamId + 1) % 2].Fishes[1];
            //fishes[2] = mission.TeamsRef[(teamId + 1) % 2].Fishes[2];
            Ball[] balls = new Ball[2];
            balls[0] = mission.EnvRef.Balls[0];
            balls[1] = mission.EnvRef.Balls[1];
            #endregion

            #region (flag=0)游到第一个球附近
            if (flag == 0)
            {
                xna.Vector3 despt1 = new xna.Vector3(-1200, 0, 700);
                xna.Vector3 despt3 = new xna.Vector3(-1000, 0, 800);

                if (Method.GetDistance(fishes[0].PositionMm, despt3) <= 150)
                {
                    flag++;

                }
                else
                {
                    Method.approachToPoint(ref decisions[0], fishes[0], despt1, 14, 12, 10);
                }
            }

            #endregion

            #region (flag=1)顶球
            if (flag == 1)
            {
                xna.Vector3 despt11 = new xna.Vector3(-600, 0, 1500);
                xna.Vector3 despt2 = new xna.Vector3(-2250, 0, 1500);
                if (Method.GetDistance(balls[0].PositionMm, despt2) <=150||Method.GetDistance(balls[0].PositionMm,despt11)<150)
                {
                    flag++;
                }
                else
                {
                    if (fishes[0].BodyDirectionRad > 0 && fishes[0].BodyDirectionRad < (Math.PI / 2.0))
                    {
                        StrategyHelper.Helpers.Dribble(ref decisions[0], fishes[0], balls[0].PositionMm, Method.GetAngle(fishes[0].PositionMm, despt11),
                    2f, 2f, 20f, 14, 12, 5, mission.CommonPara.MsPerCycle, false);
                    }
                    else
                    {
                        StrategyHelper.Helpers.Dribble(ref decisions[0], mission.TeamsRef[teamId].Fishes[0], balls[0].PositionMm, Method.GetAngle(fishes[0].PositionMm, despt2),
                           2f, 2f, 20f, 14, 12, 5, mission.CommonPara.MsPerCycle, false);//8  4  5  , 10  8   5   
                    }

                }
            }

            #endregion

            #region 


            #endregion


            #region (flag=2)前往拦截二号球位置

            if (flag == 2)
            {
                //xna.Vector3 enemy1_pt = mission.TeamsRef[(1 + teamId) % 2].Fishes[1].PositionMm;
                //xna.Vector3 enemy2_pt = mission.TeamsRef[(1 + teamId) % 2].Fishes[0].PositionMm;
                xna.Vector3 despt4 = new xna.Vector3(-500, 0, 0);
                xna.Vector3 despt5 = new xna.Vector3(-800, 0, 0);
                if (Method.GetDistance(fishes[0].PositionMm, despt5) <= 300)
                {
                    flag++;
                }
                else
                {
                    Method.approachToPoint(ref decisions[0], fishes[0], despt4, 14, 12, 3);

                }
            }

            #endregion

            #region  (flag=3)拦截二号球
            if (flag == 3)
            {
               
                
                xna.Vector3 mark4 = new xna.Vector3(-800, 0, -400);
                if (Method.GetDistance(balls[1].PositionMm, mark4) <= 500)
                {
                    flag = 31;
                }
                else if (balls[1].PositionMm.X <= 400)
                {
                    flag = 32;
                }
                
            }



            #endregion

            #region   （flag=31）顶至左上障碍左边

            if (flag == 31)
            {
                xna.Vector3 despt6 = new xna.Vector3(-600, 0, -1500);
                if (Method.GetDistance(balls[1].PositionMm, despt6) < 150)
                {
                    flag = 4;
                }
                else
                {
                    StrategyHelper.Helpers.Dribble(ref decisions[0], mission.TeamsRef[teamId].Fishes[0], balls[1].PositionMm, Method.GetAngle(fishes[0].PositionMm, despt6),
                        2f, 2f, 20f, 14, 12, 5, mission.CommonPara.MsPerCycle, false);
                }
            }

            #endregion

            #region （flag=32）顶至左上障碍右边

            if (flag == 32)
            {
                xna.Vector3 despt7 = new xna.Vector3(-500, 0, -1500);
                if (Method.GetDistance(balls[1].PositionMm, despt7) < 150)
                {
                    flag = 4;
                }
                else
                {
                    StrategyHelper.Helpers.Dribble(ref decisions[0], mission.TeamsRef[teamId].Fishes[0], balls[1].PositionMm, Method.GetAngle(fishes[0].PositionMm, despt7),
                       2f, 2f, 20f, 14, 12, 5, mission.CommonPara.MsPerCycle, false);
                }
            }

            #endregion

            #region (flag=4)再次拦截一号球(judgement)

            if (flag == 4)
            {
                xna.Vector3 despt8 = new xna.Vector3(-800, 0, 0);
                xna.Vector3 despt6 = new xna.Vector3(-600, 0, -1500);
                xna.Vector3 despt7 = new xna.Vector3(-500, 0, -1500);
                if (Method.GetDistance(balls[0].PositionMm, despt8) < 600&&Method.GetDistance(balls[1].PositionMm,despt6)<400)
                {
                    flag++;
                }
                else
                {
                    flag = 3;
                }
               
            }

            #endregion

            #region (flag=5)拦截一号球位置
            if (flag == 5)
            {
                xna.Vector3 despt8 = new xna.Vector3(-800, 0, 0);
                if (Method.GetDistance(fishes[0].PositionMm, despt8) <= 300)
                {
                    flag++;
                }
                else
                {
                    Method.approachToPoint(ref decisions[0], fishes[0], despt8, 14, 12, 3);
                }
            }
            #endregion

            #region (flag=6)拦截一号球

            if (flag == 6)
            {
                xna.Vector3 despt9 = new xna.Vector3(-500, 0, 1500);
                if (Method.GetDistance(balls[0].PositionMm, despt9) < 150)
                {
                    flag++;
                }
                else
                {
                    StrategyHelper.Helpers.Dribble(ref decisions[0], mission.TeamsRef[teamId].Fishes[0], balls[0].PositionMm, Method.GetAngle(fishes[0].PositionMm, despt9),
                      2f, 2f, 20f, 14, 12, 5, mission.CommonPara.MsPerCycle, false);
                }
            }

            #endregion

            #region (flag==7)判断二号球防守是否失手{

            if (flag == 7)
            {
                xna.Vector3 despt7 = new xna.Vector3(-500, 0, -1500);
                xna.Vector3 despt6 = new xna.Vector3(0, 0, -1500);
                if (Method.GetDistance(balls[1].PositionMm, despt7)>500|| Method.GetDistance(balls[1].PositionMm, despt6)>500)
                {
                    flag = 3;
                }
        
            }

            #endregion


            #endregion

            return decisions;
            
        }
    }
}
