import { Address } from './address';

export interface Building {
  id?: number;
  buildingName: string;
  addressId: number;
  buildingAddress?: Address;
}
