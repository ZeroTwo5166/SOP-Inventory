import { ItemGroup } from './itemGroup';
import { StatusHistory } from './statusHistory';
import { Room } from './room';
import { Loan } from './loan';
export interface Item {
  id: number;
  roomId: number;
  itemGroupId: number;
  serialNumber: string;
  itemImageUrl?: string;
  itemInfo?:string;
  statusHistories?: StatusHistory[];
  itemGroup?: ItemGroup;
  room?: Room;
  loan?: Loan;
}
