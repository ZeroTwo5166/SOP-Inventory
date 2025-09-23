import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AddressService } from '../services/address.service';
import { Address } from '../models/address';
import { NavbarComponent } from '../navbar/navbar.component';
import { Item } from '../models/item';
import { ItemService } from '../services/item.service';

@Component({
  selector: 'app-address',
  standalone: true,
  imports: [FormsModule, CommonModule, NavbarComponent],
  templateUrl: './address.component.html',
  styleUrls: ['./address.component.css'],
})
export class AddressComponent implements OnInit {
  addresses: Address[] = [];
  items: Item[] = [];

  filteredAddresses: Address[] = [];

  selectedAddress: Address | null = null;

  newAddress: Address = {
    id:0,
    zipCode: 0,
    region: '',
    city: '',
    road: '',
  };

  searchAddressTerm: string = '';

  showSuccessMessage: boolean = false;

  showCreateForm: boolean = false;

  currentUser: any;

  constructor(private addressService: AddressService, private itemService: ItemService) { }

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

  // Load when page is initialized
  ngOnInit(): void {
    this.currentUser = JSON.parse(
      localStorage.getItem('currentUser') as string
    );
    this.loadAddresses();
    this.getAllItems();
  }

  // Load all addresses from the server
  loadAddresses(): void {
    this.addressService.getAll().subscribe((data:any) => {
      this.addresses = data;
      this.filterAddresses();
    });
  }

  getAllItems(): void {
    this.itemService.getAll().subscribe({
      next: (items:any) => {
        this.items = items;
      },
      error: (error:any) => {
        console.error('Error fetching items', error);
        this.handleError(error);
      },
    });
  }

  // Filter addresses based on search term
  filterAddresses(): void {
    this.filteredAddresses = this.addresses.filter(
      (address) =>
        address.zipCode.toString().includes(this.searchAddressTerm) ||
        address.region
          .toLowerCase()
          .includes(this.searchAddressTerm.toLowerCase()) ||
        address.city
          .toLowerCase()
          .includes(this.searchAddressTerm.toLowerCase()) ||
        address.road
          .toLowerCase()
          .includes(this.searchAddressTerm.toLowerCase())
    );
  }

  // Select address to edit and copy it to selectedAddress
  selectAddress(address: Address): void {
    this.selectedAddress = { ...address };
  }

  // Create new address and add it to the server
  createAddress(): void {
    this.addressService.create(this.newAddress).subscribe(
      () => {
        this.loadAddresses();
        this.newAddress = {
          id:0,
          zipCode: 0,
          region: '',
          city: '',
          road: '',
        };
        this.showCreateForm = false;
        this.showSuccessMessage = true;
        setTimeout(() => (this.showSuccessMessage = false), 3000);
      },
      (error:any) => {
        this.handleError(error);
      }
    );
  }

  // Update selected address on the server and reload addresses
  updateAddress(): void {
    if (this.selectedAddress) {
      this.addressService.update(this.selectedAddress).subscribe(
        () => {
          this.loadAddresses();
          this.selectedAddress = null;
          this.showSuccessMessage = true;
          setTimeout(() => (this.showSuccessMessage = false), 3000);
        },
        (error:any) => {
          this.handleError(error);
        }
      );
    }
  }

  //* Disable delete button if there are items associated with the address
  public isDeleteDisabled(addressId: number): boolean {
    return this.items.some(item => item.room?.building?.buildingAddress?.id === addressId);
  }

  // Ask for confirmation before deleting address
  confirmDelete(addressId: number): void {

    if (this.items.some(item => item.room?.building?.buildingAddress?.id === addressId)) {
      alert('Kan ikke slette adresse, da der er tilknyttede genstande.');
      return; // Stop the deletion process
    }

    const confirmed = confirm('Er du sikker på, at du vil slette denne adresse?');
    if (!confirmed) return;

    this.addressService.delete(addressId).subscribe({
      next: () => this.loadAddresses(),
      error: (error:any) => this.handleError(error)
    });
  }

  // Cancel editing selected address and reset selectedAddress
  cancelEdit(): void {
    this.selectedAddress = null;
  }

  // Cancel create address
  cancelCreateForm(): void {
    this.showCreateForm = false;
  }
}
