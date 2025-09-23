import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from '../navbar/navbar.component';
import { BuildingService } from '../services/building.service';
import { AddressService } from '../services/address.service';
import { Building } from '../models/building';
import { Address } from '../models/address';
import { Item } from '../models/item';
import { ItemService } from '../services/item.service';


@Component({
  selector: 'app-building',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent],
  templateUrl: './buildings.component.html',
  styleUrls: ['./buildings.component.css'],
})
export class BuildingComponent implements OnInit {
  // Properties for the BuildingComponent class
  buildings: Building[] = [];
  filteredBuildings: Building[] = [];
  addressZipCodes: Address[] = [];
  selectedBuilding: Building | null = null;

  newBuilding: Building = { buildingName: '', addressId: 0 };
  addressList: Address[] = [];
  items: Item[] = [];

  searchBuildingTerm: string = '';

  showCreateForm: boolean = false;

  currentUser: any;


  constructor(
    private buildingService: BuildingService,
    private addressService: AddressService,
    private itemService: ItemService
  ) { }

  // Handle errors
  private handleError(error: any): void {
    switch (error.status) {
      case 0:
        alert('Ingen forbindelse til server. Prøv igen senere.');
        break;
      case 400:
        alert('Fejl i forespørgsel til serveren. Prøv igen senere.');
        break;
      case 500:
        alert('Fejl på Serveren. Prøv igen senere.');
        break;
      default:
        alert('En ukendt fejl opstod. Prøv igen senere.');
        break;
    }
  }

  // ngOnInit method for the BuildingComponent class
  ngOnInit(): void {
    this.currentUser = JSON.parse(
      localStorage.getItem('currentUser') as string
    );
    this.getBuildings();
    this.getAddresses();
    this.getAllItems();
  }

  // Method to get all buildings
  getBuildings(): void {
    this.buildingService.getAll().subscribe(
      (buildings) => {
        this.buildings = buildings;
        this.filterBuildings();
      },
      (error) => {
        this.handleError(error);
      }
    );
  }

  // Method to get all zip codes
  getAddresses(): void {
    this.addressService.getAll().subscribe(
      (addresses) => {
        this.addressList = addresses;   // each address has id + zipCode
      },
      (error) => this.handleError(error)
    );
  }

  getAllItems(): void {
    this.itemService.getAll().subscribe({
      next: (items) => {
        this.items = items;
      },
      error: (error) => {
        console.error('Error fetching items', error);
        this.handleError(error);
      },
    });
  }

  // Method to filter buildings
  filterBuildings(): void {
    this.filteredBuildings = this.buildings.filter(
      (building) =>
        building.buildingName?.toLowerCase().includes(this.searchBuildingTerm.toLowerCase()) ||
        building.addressId?.toString().includes(this.searchBuildingTerm) ||
        building.buildingAddress?.zipCode?.toString().includes(this.searchBuildingTerm)
    );

  }

  // Method to select a building
  selectBuilding(building: Building): void {
    this.selectedBuilding = { ...building };
  }

  // Method to create a building
  createBuilding(): void {
    const requestBody = {
      buildingName: this.newBuilding.buildingName,
      addressId: this.newBuilding.addressId    
    };


    this.buildingService.create(requestBody).subscribe(
      () => {
        this.getBuildings();
        this.newBuilding = { buildingName: '', addressId: 0 };
        this.showCreateForm = false;
      },
      (error) => this.handleError(error)
    );
  }


  // Method to update a building
  updateBuilding(): void {
    if (this.selectedBuilding) {
      this.buildingService.update(this.selectedBuilding).subscribe(
        () => {
          this.getBuildings();
          this.selectedBuilding = null;
        },
        (error) => {
          this.handleError(error);
        }
      );
    }
  }

  // Method to add a confirm before deleting
  confirmDelete(buildingId: number): void {
    if (this.items.some(item => item.room?.buildingId === buildingId)) {
      alert('Kan ikke slette bygningen, da der befinder sig en eller flere genstande i den.');
      return;
    }

    const confirmed = confirm('Vil du gerne slette bygningen?');
    if (!confirmed) return;

    this.buildingService.delete(buildingId).subscribe({
      next: () => this.getBuildings(),
      error: (error) => this.handleError(error)
    });
  }

  //* Disable delete button if there are items associated with the building
  public isDeleteDisabled(buildingId: number): boolean {
    return this.items.some(item => item.room?.buildingId === buildingId);
  }

  // Method to cancel an edit and set the form to null
  cancelEdit(): void {
    this.selectedBuilding = null;
  }

  // Method to show the create form and set showCreateForm to false
  cancelCreateForm(): void {
    this.showCreateForm = false;
  }
}
