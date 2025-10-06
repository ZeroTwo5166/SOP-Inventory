import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; // Import FormsModule
import { NavbarComponent } from '../navbar/navbar.component';
import { ItemService } from '../services/item.service';
import { ItemGroupService } from '../services/itemGroup.service';
import { Item } from '../models/item';
import { ItemTypeService } from '../services/itemType.service';
import { ItemType } from '../models/itemType';
import { ItemGroup } from '../models/itemGroup';
import { Router } from '@angular/router';
import { RoomService } from '../services/room.service';
import { Room } from '../models/room';
import { Building } from '../models/building';
import { Address } from '../models/address';
import { BuildingService } from '../services/building.service';
import { AddressService } from '../services/address.service';
import { PresetService } from '../services/preset.servive';
import { Preset } from '../models/preset';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [NavbarComponent, CommonModule, FormsModule],
  templateUrl: './inventory.component.html',
  styleUrls: ['./inventory.component.css'],
})
export class InventoryComponent implements OnInit {
  itemTypes: ItemType[] = [];
  itemType: ItemType = { id: 0, typeName: '', presetId: 0 };
  selectedItemTypes: { [key: number]: boolean } = {};

  items: Item[] = [];
  displayedItems: Item[] = [];
  newItem: Item = {
    id: 0,
    roomId: 0,
    itemGroupId: 0,
    serialNumber: '',
    itemImageUrl: ''

  };

  checkedItemGroup: ItemGroup = {
    id: 0,
    itemTypeId: 0,
    modelName: '',
    quantity: 0,
    price: 0,
    manufacturer: '',
    warrantyPeriod: '',
    itemType: {
      id: 0,
      typeName: '',
      presetId: 0
    },
  };

  selectedItem: Item = {
    id: 0,
    roomId: 0,
    itemGroupId: 0,
    serialNumber: '',
    itemImageUrl: ''
  };

  selectedItemGroup: ItemGroup = {
    id: 0,
    itemTypeId: 0,
    modelName: '',
    quantity: 0,
    price: 0,
    manufacturer: '',
    warrantyPeriod: '',
    itemType: {
      id: 0,
      typeName: '',
      presetId: 0
    },
  };

  itemGroups: ItemGroup[] = [];
  selectedItemGroups: ItemGroup[] = [];
  selectedIds: number[] = [];

  rooms: Room[] = [];
  buildings: Building[] = [];
  addresses: Address[] = [];

  isVisible = false;
  showModal: boolean = false;
  showEditModal: boolean = false;
  isClicked = false;

  searchItemGroup: string = '';
  filteredItemGroups: ItemGroup[] = [];

  searchItem: string = '';
  filteredItems: Item[] = [];

  currentUser: any;
  archiveNote: string = '';
  showErrorNote: boolean = false;
  showArchiveModal: boolean = false;

  selectedImage: File | null = null;
  selectedImagePreview: string | null = null;
  isUploadingImage: boolean = false;
  imageUpdated: boolean = false;
  uploadError: boolean = false;

  showInfoModal: boolean = false;


  // holds the selected itemGroup object (not just the id)
  selectedItemGroupObj: ItemGroup | null = null;

  // preset UI helpers
  presetJsonString: string = '';
  showRawPreset = false;


  presets: any[] = [];
  selectedPresetData: any = null;
  presetFields: { [key: string]: any } = {}; // Store user input for preset fields
  objectKeys = Object.keys;

  constructor(
    private itemService: ItemService,
    private itemGroupService: ItemGroupService,
    private itemTypeService: ItemTypeService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private roomService: RoomService,
    private buildingService: BuildingService,
    private addressService: AddressService,
    private presetService: PresetService // Add this

  ) { }

  ngOnInit(): void {
    this.currentUser = JSON.parse(
      localStorage.getItem('currentUser') as string
    );
    this.fetchData();
    this.fetchItems();
    this.getRooms();
    this.getBuildings();
    this.getAddresses();
  }


  onItemGroupChange() {
    console.log("Item Group Changed");

    const groupId = +this.newItem.itemGroupId; // convert to number

    const selectedGroup = this.itemGroups.find(ig => ig.id === groupId);

    console.log(selectedGroup)
    console.log(this.itemGroups)
    if (selectedGroup && selectedGroup.itemType) {
          console.log("Item Type:", selectedGroup.itemType);
        const presetId = selectedGroup.itemType.presetId;
        
        if (presetId && presetId > 0) {
          // Fetch preset data based on presetId
          this.fetchPresetData(presetId);
        } else {
          // No preset for this item type
          this.selectedPresetData = null;
          this.presetFields = {};
        }
      }
    

  }


  fetchPresetData(presetId: number): void {
    this.presetService.findById(presetId).subscribe(
      (preset) => {
        console.log("Fetched Preset:", preset);
        this.selectedPresetData = preset;
        
        // Initialize preset fields with empty values for user input
        this.presetFields = {};
        if (preset) {
          Object.keys(preset).forEach(key => {
            // Skip id and other metadata fields
            if (key !== 'id' && key !== 'presetId') {
              this.presetFields[key] = '';
            }
          });
        }
        this.cdr.detectChanges();
      },
      (error) => {
        console.error('Error fetching preset data:', error);
        this.selectedPresetData = null;
        this.presetFields = {};
      }
    );
  }




  // ============================
  // Fetch data from database
  // ============================

  // Fetch all ItemTypes and all ItenGroups from the database
  fetchData(): void {
    this.itemTypeService.getAll().subscribe((x) => (this.itemTypes = x));
    this.itemGroupService.getAll().subscribe((itemGroup) => {
      this.itemGroups = itemGroup;
      this.updateDisplayedItems();
    });
  }

  // Fetch all Rooms from the database
  getRooms(): void {
    this.roomService.getAll().subscribe((rooms) => (this.rooms = rooms));
  }

  // Fetch all Buildings from the database
  getBuildings(): void {
    this.buildingService
      .getAll()
      .subscribe((buildings) => (this.buildings = buildings));
  }

  // Fetch all Addresses from the database
  getAddresses(): void {
    this.addressService
      .getAll()
      .subscribe((addresses) => (this.addresses = addresses));
  }

  // Fetch all Items from the database
  fetchItems(): void {
    this.itemService.getAll().subscribe((x) => (this.items = x));
  }

  // Helper method for room number
  getRoomNumber(roomId: number): string {
    if (!roomId) return 'Ikke angivet';

    const room = this.rooms.find((r) => r.id === roomId);
    return room ? room.roomNumber.toString() : 'Rum ikke fundet';
  }

  // Helper method for building name
  getBuildingInfo(roomId: number): string {
    if (!roomId) return 'Ikke angivet';

    const room = this.rooms.find((r) => r.id === roomId);
    if (!room) return 'Bygning ikke fundet';

    const building = this.buildings.find((b) => b.id === room.buildingId);
    return building ? building.buildingName : 'Bygning ikke fundet';
  }

  //Image methods
  resetImage() {
    this.selectedImagePreview = null;
    (document.getElementById('itemImage') as HTMLInputElement).value = '';
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.selectedImage = input.files[0];


      const reader = new FileReader();
      reader.onload = () => {
        this.selectedImagePreview = reader.result as string;
      };
      reader.readAsDataURL(this.selectedImage);

      // Reset upload states
      this.imageUpdated = false;
      this.uploadError = false;
    }
  }


  uploadImageToCloudinaryAsync(file: File): Promise<string> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('upload_preset', 'SOP_ProfileImages');

    const cloudName = 'dkrcapzct';
    const uploadUrl = `https://api.cloudinary.com/v1_1/${cloudName}/image/upload`;

    return fetch(uploadUrl, {
      method: 'POST',
      body: formData,
    })
      .then(res => {
        if (!res.ok) {
          throw new Error(`HTTP error! status: ${res.status}`);
        }
        return res.json();
      })
      .then(data => {
        return data.secure_url;
      });
  }

  // Helper method for address
  getAddressInfo(roomId: number): string {
    if (!roomId) return 'Ikke angivet';

    const room = this.rooms.find((r) => r.id === roomId);
    if (!room) return 'Adresse ikke fundet';

    const building = this.buildings.find((b) => b.id === room.buildingId);
    if (!building) return 'Adresse ikke fundet';

    const address = this.addresses.find((a) => a.id === building.buildingAddress?.id); //TEST

    return address ? address.road : 'Adresse ikke fundet';
  }

  // ============================
  // Filtering/sorting and displaying Items and Item Groups
  // ============================

  // Refresh filters for selected things
  refreshFilter(): void {
    this.selectedItemTypes = {};
    this.selectedIds = [];
    this.selectedItemGroups = this.itemGroups;
    this.isVisible = false;
    this.isClicked = false;
  }

  // Filter the item groups for display
  updateDisplayedItems(): void {
    // Filter by selected itemTypeIds
    let filteredByCheckbox =
      this.selectedIds.length > 0
        ? this.itemGroups.filter((x) => this.selectedIds.includes(Number(x.itemTypeId)))
        : this.itemGroups;

    // Further filter by search bar input
    if (this.searchItemGroup && this.searchItemGroup.trim() !== '') {
      filteredByCheckbox = filteredByCheckbox.filter((itemGroup) =>
        itemGroup.modelName
          .toLowerCase()
          .includes(this.searchItemGroup.toLowerCase())
      );
    }

    // Set the filtered result
    this.selectedItemGroups = filteredByCheckbox;
    this.cdr.detectChanges(); // Manually trigger change detection if needed
  }

  // Resets visibility and updates displayed items
  IsVisible(): void {
    this.updateDisplayedItems();
    this.isVisible = false;
    this.isClicked = false;
  }

  // Sorts out all the items that has the same selected Item Group id
  sortItems(): void {
    this.selectedItemGroups = this.itemGroups.filter(
      (i) => i.id === this.selectedItemGroup.id
    );
    this.displayedItems = this.items.filter(
      (item) => item.itemGroupId === this.selectedItemGroup.id
    );
    this.onItemSearch();
  }

  // Metode to show only the searched items
  onItemSearch(): void {
    this.filteredItems = this.displayedItems.filter(
      (item) =>
        item.id.toString().includes(this.searchItem) ||
        item.serialNumber.toLowerCase().includes(this.searchItem.toLowerCase())
    );
  }

  // This metode check if any item type is checked, and filters the item groups accordingly
  onChecked(id: number, event: any): void {
    if (event.target.checked) {
      this.selectedItemTypes[id] = true;
    } else {
      delete this.selectedItemTypes[id];
    }

    // Update selectedIds to match keys in selectedItemTypes
    this.selectedIds = Object.keys(this.selectedItemTypes).map((key) => Number(key));

    this.updateDisplayedItems();
    this.isVisible = false;
  }

  // This metode shows the items attached to the clicked item group
  onItemGroupClick(itemGroup: ItemGroup, event: any): void {
    // If its clicked first shows the things and if its click again it sets the things invisible
    if (!this.isClicked) {
      this.fetchItems();
      this.selectedItemGroup = itemGroup;
      this.sortItems();
      this.isVisible = true;
      this.isClicked = true;
    } else {
      this.updateDisplayedItems();
      this.isVisible = false;
      this.isClicked = false;
    }
  }

  // ============================
  // Create, update and delete metodes
  // ============================



  // Creates new item
  async createNewItem(): Promise<void> {
    try {


      // If an image is selected, upload it first
      if (this.selectedImage) {
        this.isUploadingImage = true;
        this.newItem.itemImageUrl = await this.uploadImageToCloudinaryAsync(this.selectedImage);
        this.isUploadingImage = false;
      }


      // Call the item service to create a new item and wait for the response
      await this.itemService.create(this.newItem).toPromise();

      // Close the modal for creating new items
      this.closeNewItemModal();

      // Reset the newItem object to its default state for future use
      this.newItem = { id: 0, itemGroupId: 0, roomId: 0, serialNumber: '', itemImageUrl: '' };
      this.selectedImage = null;
      this.selectedImagePreview = null;

      // Reload the entire page to reflect changes
      window.location.reload();
    } catch (error) {
      this.isUploadingImage = false;
      // Log any errors encountered during the item creation process
      console.error('Error creating item type', error);
    }
  }

  // Edits the selected item
  async editItem(): Promise<void> {
    // Ensure that there is a selected item to edit
    if (!this.selectedItem) return;

    // Send an update request to the item service
    this.itemService.update(this.selectedItem).subscribe(
      async (response) => {
        // Destructure necessary properties from the response
        const { id: itemId, itemGroup } = response;
        const itemType = itemGroup?.itemType?.typeName;

        // Close the modal for editing item items
        this.closeEditItemModal();

        // Reset the selected item to its default state
        this.selectedItem = {
          id: 0,
          roomId: 0,
          itemGroupId: 0,
          serialNumber: '',
          itemImageUrl: ''
        };

        // Reload the page to reflect the changes (Consider updating the UI dynamically instead)
        window.location.reload();
      },
      (error) => {
        // Log any errors that occur during the update process
        console.error('Error updating item', error);
      }
    );
  }


  showInformation(): void {
    this.showInfoModal = !this.showInfoModal;
  }


  // ==========================
  // Open and close models and reroute
  // ============================

  // Route to selected item details page
  onLineClick(item: Item, event: any): void {
    this.router.navigate(['/itemDetails', item.id]);
  }

  // Open New Item Modal
  openNewItemModal(): void {
    this.showModal = true;
  }

  // Close New Item Modal
  closeNewItemModal(): void {
    this.showModal = false;
  }

  // Fix the openEditItemModal method
  openEditItemModal(item: Item): void {
    this.selectedItem = { ...item };

    // Debug: Log the item to see what properties it actually has
    console.log('Item object:', item);
    console.log('Item image URL:', item.itemImageUrl);

    // Try both possible property names to be safe
    const imageUrl = item.itemImageUrl || item.itemImageUrl || '';

    // If the item has an existing image, set it as the preview
    if (imageUrl && imageUrl.trim() !== '') {
      this.selectedImagePreview = imageUrl;
      console.log('Setting image preview to:', imageUrl);
    } else {
      this.selectedImagePreview = null;
      console.log('No image URL found');
    }

    // Reset the file input and selected image since we're showing existing image
    this.selectedImage = null;
    this.imageUpdated = false;
    this.uploadError = false;

    this.showEditModal = true;
  }

  // Close Edit Modal and reset selectedItem
  closeEditItemModal(): void {
    this.showEditModal = false;
    this.selectedItem = {
      id: 0,
      roomId: 0,
      itemGroupId: 0,
      serialNumber: '',
      itemImageUrl: ''
    };

    // Reset image-related properties
    this.selectedImage = null;
    this.selectedImagePreview = null;
    this.imageUpdated = false;
    this.uploadError = false;

    // Reset the file input
    const fileInput = document.getElementById('editItemImage') as HTMLInputElement;
    if (fileInput) {
      fileInput.value = '';
    }


  }


  confirmArchiveItem(): void {

    if (this.items.some(item => item.itemGroup?.id === this.selectedItemGroup?.id && item.loan !== null)) {
      alert('Kan ikke arkivere denne type, da der er lån tilknyttet.');
      return;
    }

    if (!this.archiveNote.trim()) {
      this.showErrorNote = true;
      return;
    }

    if (!this.selectedItem?.id) {
      return;
    }

    this.itemService.delete(this.selectedItem.id, this.archiveNote).subscribe({
      next: () => {
        window.location.reload();
      },
      error: () => {
      },
    });
  }

  //* Disable delete button if there are loans associated with the item
  public isDeleteDisabled(itemId: number): boolean {
    return this.items.some(item => item.id === itemId && item.loan !== null);
  }

  openArchiveModal(item?: Item): void {
    if (!item) return;  // Prevent opening if no item selected

    this.selectedItem = { ...item };
    this.showArchiveModal = true;   // Show modal
  }

  closeArchiveModal(): void {
    this.showArchiveModal = false;  // Hide modal
    this.archiveNote = '';
    this.showErrorNote = false;
  }
}




















