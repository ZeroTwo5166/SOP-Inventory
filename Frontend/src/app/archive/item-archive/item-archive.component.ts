import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from '../../navbar/navbar.component';
import { ArchiveItem } from '../archive-models/archive-item';
import { ArchiveItemService } from '../archive-services/archive-item.service';
import { ItemGroupService } from '../../services/itemGroup.service';
import { ItemGroup } from '../../models/itemGroup';
import { ArchiveItemGroup } from '../archive-models/archive-itemgroup';
import { ArchiveItemGroupService } from '../archive-services/archive-itemgroup.service';
import { Address } from '../../models/address';
import { AddressService } from '../../services/address.service';
import { Building } from '../../models/building';
import { BuildingService } from '../../services/building.service';
import { Room } from '../../models/room';
import { RoomService } from '../../services/room.service';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-item-archive',
  imports: [FormsModule, CommonModule, NavbarComponent, RouterModule],
  templateUrl: './item-archive.component.html',
  styleUrl: './item-archive.component.css',
})
export class ItemArchiveComponent implements OnInit {
  archiveItems: ArchiveItem[] = [];

  filteredArchivedItems: ArchiveItem[] = [];
  itemGroups: ItemGroup[] = [];
  archivedItemGroups: ArchiveItemGroup[] = [];

  addresses: Address[] = [];
  buildings: Building[] = [];
  rooms: Room[] = [];

  searchArchivedItemTerm: string = '';
  
  constructor(
    private archiveItemService: ArchiveItemService,
    private itemGroupService: ItemGroupService,
    private archiveItemGroupService: ArchiveItemGroupService,
    private addressService: AddressService,
    private buildingService: BuildingService,
    private roomService: RoomService
  ) { }

  ngOnInit(): void {
    this.fetchItems();
    this.fetchItemGroups();
    this.fetchAddresses();
    this.fetchBuildings();
    this.fetchRooms();
  }

  fetchItems(): void {
    this.archiveItemService.getAll().subscribe({
      next: (items) => {
        this.archiveItems = items;
        this.filteredArchivedItems = [...items];
      },
      error: (error) => {
        console.error('Error fetching archive items:', error);
      },
    });
  }

  fetchArchivedItemGroups(): void {
    this.archiveItemGroupService.getAll().subscribe({
      next: (archivedItemGroups) => {
        this.archivedItemGroups = archivedItemGroups;
      },
      error: (error) =>
        console.error('Error fetching archived item groups:', error),
    });
  }

  fetchAddresses(): void {
    this.addressService.getAll().subscribe({
      next: (addresses) => {
        this.addresses = addresses;
      },
      error: (error) => console.error('Error fetching addresses:', error),
    });
  }

  fetchBuildings(): void {
    this.buildingService.getAll().subscribe({
      next: (buildings) => {
        this.buildings = buildings;
      },
      error: (error) => console.error('Error fetching buildings:', error),
    });
  }

  fetchRooms(): void {
    this.roomService.getAll().subscribe({
      next: (rooms) => {
        this.rooms = rooms;
      },
      error: (error) => console.error('Error fetching rooms:', error),
    });
  }

  fetchItemGroups(): void {
    this.itemGroupService.getAll().subscribe({
      next: (itemGroups) => {
        this.itemGroups = itemGroups;
        this.fetchArchivedItemGroups();
      },
      error: (error) => {
        console.error('Error fetching item groups:', error);
        this.fetchArchivedItemGroups();
      },
    });
  }

  // checking the archive table and normal table. for modelname to display //
  getItemGroupName(itemGroupId: number): string {
    const itemGroup = this.itemGroups.find((group) => group.id === itemGroupId);
    if (itemGroup) {
      return itemGroup.modelName;
    }

    const archivedItemGroup = this.archivedItemGroups.find(
      (group) => group.id === itemGroupId
    );
    if (archivedItemGroup) {
      return `${archivedItemGroup.modelName} (Arkiveret)`;
    }

    return 'Gruppe ikke fundet';
  }
  
  getRoomLocation(roomId: number): string {
    const room = this.rooms.find((r) => r.id === roomId);
    if (room) {
      const building = this.buildings.find((b) => b.id === room.buildingId);
      if (building) {
        const address = this.addresses.find((a) => a.zipCode === building.addressId); //TEST
        if (address) {
          return ` ${room.roomNumber}, ${building.buildingName}, ${address.road}`;
        } else {
          return ` ${room.roomNumber}, ${building.buildingName} (Adresse ikke fundet)`;
        }
      } else {
        return ` ${room.roomNumber} (Bygning ikke fundet)`;
      }
    } else {
      return 'Lokation ikke fundet';
    }
  }


  filterArchivedItems(): void {
    this.filteredArchivedItems = this.archiveItems.filter(
      (item) =>
        item.serialNumber.toLowerCase().includes(this.searchArchivedItemTerm.toLowerCase())
    );
  }

  confirmDelete(id: number) {
    if (confirm('Er du sikker på at du vil slette denne genstand permanent?')) {
      this.archiveItemService.delete(id).subscribe({
        next: () => {
          this.archiveItems = this.archiveItems.filter(
            (item) => item.id !== id
          );
          window.location.reload();
        },
        error: (error) => {
          console.error('Error deleting archive item:', error);
        },
      });
    }
  }

  // Method for restoring by ID. //
  restoreItem(id: number) {
    if (confirm('Er du sikker på at du vil gendanne denne gendstand?')) {
      // First get the itemgroup to check its itemTypeId
      const item = this.archiveItems.find((ig) => ig.id === id);

      if (!item) {
        alert('Gendstanden blev ikke fundet.');
        return;
      }

      // Check if itemGroup exists
      this.itemGroupService.findById(item.itemGroupId).subscribe({
        next: (_itemGroup) => {
          // itemGroup exists, proceed with restore
          this.archiveItemService.restore(id).subscribe({
            next: () => {
              this.archiveItems = this.archiveItems.filter(
                (item) => item.id !== id
              );
              this.fetchItems();
            },
            error: (error) => {
              console.error('Error restoring item:', error);
              alert('Der opstod en fejl ved gendannelse af genstanden.');
            },
          });
        },
        // If itemGroup does not exist, show an alert
        error: (error) => {
          console.error('Error checking itemgroup:', error);
          alert(
            'Genstandsgruppen findes ikke længere. Genstanden kan ikke gendannes.'
          );
        },
      });
    }
  }
}
