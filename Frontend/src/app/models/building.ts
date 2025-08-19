import { Address } from './address';

export interface Building {
  id: number;
  buildingName: string;
  AddressId: number;
  buildingAddress?: Address;
}
