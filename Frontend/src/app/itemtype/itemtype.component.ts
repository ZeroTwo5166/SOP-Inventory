import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavbarComponent } from '../navbar/navbar.component';
import { Item } from '../models/item';
import { ItemType } from '../models/itemType';
import { ItemTypeService } from '../services/itemType.service';
import { ItemService } from '../services/item.service';

@Component({
  selector: 'app-itemtype',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent],
  templateUrl: './itemtype.component.html',
  styleUrl: './itemtype.component.css',
})
export class ItemtypeComponent {
  itemTypes: ItemType[] = [];
  items: Item[] = [];
  newItemType: ItemType = {
    id: 0,
    typeName: '',
  };
  selectedItemType: ItemType | null = null;
  archiveNote: string = '';
  showErrorNote: boolean = false;
  currentUser: any;
  constructor(private itemTypeService: ItemTypeService, private itemService: ItemService) { }

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

  ngOnInit(): void {
    this.currentUser = JSON.parse(
      localStorage.getItem('currentUser') as string
    );
    this.getItemTypes();
    this.getAllItems();
  }


  onSubmit(): void {
    if(this.newItemType.typeName){
      this.createItemType();

    }
  }

  // Method for getting all item types. \\
  getItemTypes(): void {
    this.itemTypeService.getAll().subscribe({
      next: (itemTypes) => {
        this.itemTypes = itemTypes;
      },
      error: (error) => {
        console.error('Error fetching item types', error);
        this.handleError(error);
      },
    });
  }

  // Method for getting all items. Is needed to perfom loancheck\\
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

  // Method for creating a new item type. \\
  createItemType(): void {
    this.itemTypeService.create(this.newItemType).subscribe({
      next: (itemType) => {
        this.itemTypes.push(itemType);
        this.newItemType = {
          id: 0,
          typeName: '',
        };
      },
      error: (error) => {
        console.error('Error creating item type', error);
        this.handleError(error);
      },
    });
  }

  // Method for archiving an item type by ID.
  confirmArchiveItemType(): void {

    if (this.items.some(item => item.itemGroup?.itemType?.id === this.selectedItemType?.id && item.loan !== null)) {
      alert('Kan ikke arkivere denne type, da der er lån tilknyttet.');
      return;
    }


    if (!this.archiveNote.trim()) {
      this.showErrorNote = true;
      return;
    }

    if (!this.selectedItemType?.id) {
      console.error('Nothing selected');
      return;
    }

    this.itemTypeService.delete(this.selectedItemType.id, this.archiveNote).subscribe({
      next: () => {
        this.getItemTypes();
        this.closeArchiveModal();
      },
      error: (error) => {
        this.handleError(error);
      },
    });
  }

  //* Disable delete button if there are loans associated with the item type
  public isDeleteDisabled(itemTypeId: number): boolean {
    return this.items.some(item => item.itemGroup?.itemType?.id === itemTypeId && item.loan !== null);
  }

  openArchiveModal(itemType: ItemType): void {
    this.selectedItemType = itemType;
    const modal = document.getElementById('ArhciveModal');
    if (modal) {
      modal.style.display = 'block';
    }
  }

  closeArchiveModal(): void {
    const modal = document.getElementById('ArhciveModal');
    if (modal) {
      modal.style.display = 'none';
    }
    this.archiveNote = '';
    this.showErrorNote = false;
  }
}
