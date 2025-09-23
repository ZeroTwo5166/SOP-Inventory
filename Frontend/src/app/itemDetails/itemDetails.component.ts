import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar.component';
import { ItemService } from '../services/item.service';
import { Item } from '../models/item';
import { ItemType } from '../models/itemType';
import { ItemTypeService } from '../services/itemType.service';
import { StatusHistory } from '../models/statusHistory';
import { firstValueFrom } from 'rxjs';
import { Status } from '../models/status';
import { StatusService } from '../services/status.service';
import { StatusHistoryService } from '../services/statusHistory.service';
import { User } from '../models/user';
import { UserService } from '../services/user.service';
import { RoomService } from '../services/room.service';
import { ItemGroupService } from '../services/itemGroup.service';
import { DomSanitizer, SafeStyle } from '@angular/platform-browser';
import { Room } from '../models/room';
import { ItemGroup } from '../models/itemGroup';

@Component({
  selector: 'app-itemDetails',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent],
  templateUrl: './itemDetails.component.html',
  styleUrls: ['./itemDetails.component.css'],
})
export class ItemDetailsComponent implements OnInit {
  // Variables used in item details. \\
  item: Item = {
    id: 0,
    roomId: 0,
    itemGroupId: 0,
    serialNumber: '',
    statusHistories: [],
    itemGroup: {
      id: 0,
      modelName: '',
      itemTypeId: 0,
      quantity: 0,
      price: 0,
      manufacturer: '',
      warrantyPeriod: '',
    },
    room: {
      id: 0,
      buildingId: 0,
      roomNumber: 0,
      building: {
        id: 0,
        buildingName: '',
        addressId:0,
        buildingAddress: {
          id:0,
          zipCode: 0,
          road: '',
          region: '',
          city: '',
        },
      },
    },
    loan: {
      id: 0,
      userId: 0,
      itemId: 0,
      loanDate: new Date(),
      returnDate: new Date(),
    },
  };

  itemType: ItemType = { id: 0, typeName: '' };

  statuses: Status[] = [];
  status: Status = { id: 0, name: '' };
  itemGroups: ItemGroup[] = [];
  rooms: Room[] = [];

  user: User = {
    id: 0,
    name: '',
    email: '',
    password: '',
    twoFactorAuthentication: false,
    roleId: 0,
    role: undefined,
  };

  selectedItem: Item = {
    id: 0,
    roomId: 0,
    itemGroupId: 0,
    serialNumber: '',
  };

  newStatusHistory: StatusHistory = {
    id: 0,
    itemId: 0,
    statusId: 0,
    statusUpdateDate: new Date(),
    note: '',
  };

  currentStatusHistory: StatusHistory = {
    id: 0,
    itemId: 0,
    statusId: 0,
    statusUpdateDate: new Date(),
    note: '',
  };

  isComputer = false;
  collapsedItemGroup = false;
  collapsedStatusHistories = false;
  collapsedComputerParts = false;
  collapsedState: { [key: number]: boolean } = {};

  showEditModal: boolean = false;
  showQRCodeModal: boolean = false;
  showStatusModal: boolean = false;

  statusCache = new Map<number, string>(); // Cache for status names

  qrCodeStyle: SafeStyle | undefined;
  qrCodeUrl: string = ''; // Raw QR code URL
  inputUrl: string = ''; // User input URL

  constructor(
    private itemService: ItemService,
    private itemTypeService: ItemTypeService,
    private statusService: StatusService,
    private statusHistoryService: StatusHistoryService,
    private userService: UserService,
    private roomService: RoomService,
    private itemGroupService: ItemGroupService,
    private route: ActivatedRoute,
    private sanitizer: DomSanitizer,
    private router: Router
  ) {}

  async ngOnInit(): Promise<void> {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.inputUrl = window.location.origin + this.router.url;
    this.getItemGroups();
    this.getRooms();
    this.getStatuses();

    await this.fetchItem(id);
    await this.fetchItemType(this.item);
    if (this.item.loan?.userId !== undefined) {
      await this.fetchUser(this.item.loan?.userId);
    }
  }

  // ============================
  // Fetch data from database
  // ============================

  // Method to get all rooms. \\
  getRooms(): void {
    this.roomService.getAll().subscribe((rooms) => (this.rooms = rooms));
  }

  // Method to get all statuses
  getStatuses(): void {
    this.statusService
      .getAll()
      .subscribe((statuses) => (this.statuses = statuses));
  }

  // Method to get all item groups
  getItemGroups(): void {
    this.itemGroupService.getAll().subscribe((itemGroup) => {
      this.itemGroups = itemGroup;
    });
  }

  // Method to get the specific item
  async fetchItem(id: number): Promise<void> {
    const data = await firstValueFrom(this.itemService.findById(id));
    this.item = data;
    if (data.statusHistories != undefined) {
      // Set the last item as CurrentStatusHistory, if the list is not empty
      if (data.statusHistories.length > 0) {
        this.currentStatusHistory =
          data.statusHistories[data.statusHistories.length - 1];
      }
    }
  }

  // Method to get the specific item type
  async fetchItemType(item: Item): Promise<void> {
    if (item.itemGroup != undefined) {
      if (item.itemGroup.itemTypeId != null) {
        this.itemTypeService
          .findById(item.itemGroup.itemTypeId)
          .subscribe((data: ItemType) => {
            this.itemType = data;
          });
      }
    }
  }

  // Metode to get the specific user
  async fetchUser(id: number): Promise<void> {
    try {
      this.user = await firstValueFrom(this.userService.findById(id));
    } catch (e) {
      // console.error(e);
    }
  }

  // Helper method to get status name from ID
  getStatusName(statusHistory: any): string {
    if (!statusHistory || !this.statuses) {
      return 'Status ikke fundet';
    }

    const status = this.statuses.find((s) => s.id === statusHistory.statusId);
    return status ? status.name : 'Status ikke fundet';
  }

  // Method to load item with all related entities
  loadItemDetails(id: number): void {
    this.itemService.findById(id).subscribe({
      next: (item) => {
        this.item = item;

        // Load additional details like user info for loans
        if (this.item.loan && this.item.loan.userId) {
          this.loadUserDetails(this.item.loan.userId);
        }

        // Load item type info
        if (this.item.itemGroup && this.item.itemGroup.itemTypeId) {
          this.loadItemTypeDetails(this.item.itemGroup.itemTypeId);
        }

        // Get current/latest status
        if (this.item.statusHistories && this.item.statusHistories.length > 0) {
          this.currentStatusHistory = [...this.item.statusHistories].sort(
            (a, b) =>
              new Date(b.statusUpdateDate).getTime() -
              new Date(a.statusUpdateDate).getTime()
          )[0];
        }
      },
      error: (error) => console.error('Error loading item details:', error),
    });
  }

  // Load user details for loans
  loadUserDetails(userId: number): void {
    this.userService.findById(userId).subscribe({
      next: (user) => (this.user = user),
      error: (error) => console.error('Error loading user details:', error),
    });
  }

  // Load item type details
  loadItemTypeDetails(itemTypeId: number): void {
    this.itemTypeService.findById(itemTypeId).subscribe({
      next: (itemType) => (this.itemType = itemType),
      error: (error) =>
        console.error('Error loading item type details:', error),
    });
  }

  // Edits the item
  async editItem(): Promise<void> {
    if (!this.selectedItem) return;

    console.log('Updating item:', this.selectedItem);

    // Update the item
    this.itemService.update(this.selectedItem).subscribe(
      async (response) => {
        console.log('Item updated successfully:', response);

        const { id: itemId, itemGroup } = response;
        const itemType = itemGroup?.itemType?.typeName;

        this.closeEditModal();
        this.selectedItem = {
          id: 0,
          roomId: 0,
          itemGroupId: 0,
          serialNumber: '',
        };
        this.ngOnInit();
      },
      (error) => {
        console.error('Error updating item', error);
      }
    );
  }

  // Creates a new status history
  createStatusHistory(): void {
    this.statusHistoryService.create(this.newStatusHistory).subscribe(() => {
      window.location.reload();
    });
  }

  // Resets new Status history and closes the status modal
  cancelStatus(): void {
    this.newStatusHistory = {
      id: 0,
      itemId: 0,
      statusId: 0,
      statusUpdateDate: new Date(),
      note: '',
    };
    this.closeStatusModal();
  }

  // Makes the QR code using quickchart api, and putting the url into it
  makeQRCode(): void {
    this.showQRCodeModal = true;
    const encodedUrl = encodeURIComponent(this.inputUrl);
    const apiUrl = `https://quickchart.io/qr?text=${encodedUrl}&size=300`;

    // Sanitize the URL for safe rendering in Angular
    this.qrCodeStyle = this.sanitizer.bypassSecurityTrustStyle(
      `url(${apiUrl})`
    );
    this.qrCodeUrl = `https://quickchart.io/qr?text=${encodeURIComponent(
      this.inputUrl
    )}&size=300`;
  }

  // Saves QR code as an image (png)
  saveQRCodeAsImage(): void {
    const qrCodeUrl = this.qrCodeUrl; // Ensure you have the QR code URL here

    if (!qrCodeUrl) {
      console.error('QR Code URL not found!');
      return;
    }

    fetch(qrCodeUrl, { mode: 'cors' })
      .then((response) => {
        if (!response.ok) {
          throw new Error('Failed to fetch QR Code image');
        }
        return response.blob();
      })
      .then((blob) => {
        const image = new Image();
        const url = URL.createObjectURL(blob);

        image.onload = () => {
          // Create a canvas element
          const canvas = document.createElement('canvas');
          const context = canvas.getContext('2d');

          // Set canvas dimensions
          canvas.width = image.width;
          canvas.height = image.height;

          // Draw image on canvas
          context?.drawImage(image, 0, 0);

          // Convert canvas to data URL
          const imageUrl = canvas.toDataURL('image/png');

          // Trigger download
          const downloadLink = document.createElement('a');
          downloadLink.href = imageUrl;
          downloadLink.download = 'QRCode.png';
          downloadLink.click();

          // Revoke the temporary URL
          URL.revokeObjectURL(url);
        };

        image.src = url; // Set the image source to the blob URL
      })
      .catch((error) => console.error('Error saving QR code as image:', error));
  }

  // ==========================
  // Open, close models, rerouting, and toggle for collapsable parts
  // ============================

  // Open Edit modal with the selected item
  openEditModal(item: Item): void {
    this.selectedItem = { ...item };
    this.showEditModal = true;
  }

  // Close Edit modal and reset selected item
  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedItem = {
      id: 0,
      roomId: 0,
      itemGroupId: 0,
      serialNumber: '',
    };
  }

  // Open status modal and reset the new status
  openStatusModal(): void {
    this.showStatusModal = true;
    this.newStatusHistory = {
      id: 0,
      itemId: this.item.id,
      statusId: 0,
      statusUpdateDate: new Date(),
      note: '',
    };
  }

  // Close status modal
  closeStatusModal(): void {
    this.showStatusModal = false;
  }

  // Close QR Modal
  closeQRModal(): void {
    this.showQRCodeModal = false;
  }

  // Toggle ItemG Group
  toggleItemGroup(): void {
    this.collapsedItemGroup = !this.collapsedItemGroup;
  }

  // Togle Status Histories
  toggleStatusHistories(): void {
    this.collapsedStatusHistories = !this.collapsedStatusHistories;
  }

  // Togle Computer parts
  toggleComputerParts(): void {
    this.collapsedComputerParts = !this.collapsedComputerParts;
  }

  // Togle enkelt computer part
  toggleCollapsePart(id: number): void {
    this.collapsedState[id] = !this.collapsedState[id];
  }

  // Rerouting to computer side with the computer id
  addPartToThisComputer(Id: number): void {
    this.router.navigate(['/computer']);
  }
}
