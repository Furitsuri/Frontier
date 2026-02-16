using UnityEngine;

namespace Frontier.Stage
{
    public class StageDirectionConverter
    {
        BattleCameraController _btlCameraCtrl;

        public void Regist( BattleCameraController btlCameraCtrl )
        {
            _btlCameraCtrl = btlCameraCtrl;
        }

        public Direction Convert( Direction dir )
        {
            float angleXZ = ( _btlCameraCtrl.AngleXZ + 360f ) % 360f;

            Direction retDir = dir;
            Direction[] dirs;

            switch( dir )
            {
                case Direction.FORWARD:
                    dirs = new Direction[] { Direction.RIGHT, Direction.BACK, Direction.LEFT, Direction.FORWARD };
                    break;
                case Direction.RIGHT:
                    dirs = new Direction[] { Direction.BACK, Direction.LEFT, Direction.FORWARD, Direction.RIGHT };
                    break;
                case Direction.LEFT:
                    dirs = new Direction[] { Direction.FORWARD, Direction.RIGHT, Direction.BACK, Direction.LEFT };
                    break;
                case Direction.BACK:
                    dirs = new Direction[] { Direction.LEFT, Direction.FORWARD, Direction.RIGHT, Direction.BACK };
                    break;
                default:
                    return Direction.NONE;
            }

            if( 45f < angleXZ && angleXZ <= 135f )
            {
                retDir = dirs[0];
            }
            else if( 135f < angleXZ && angleXZ <= 225f )
            {
                retDir = dirs[1];
            }
            else if( 225f < angleXZ && angleXZ <= 315f )
            {
                retDir = dirs[2];
            }
            else
            {
                retDir = dirs[3];
            }

            return retDir;
        }
    }
}