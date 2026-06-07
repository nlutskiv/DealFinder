import { CatalogOption } from '../models/catalog-option';

export const CATALOG_OPTIONS: CatalogOption[] = [
  // GPUs
  {
    category: 'gpu',
    label: 'RTX 3080',
    query: 'RTX 3080',
    description: 'High-end 1440p / entry 4K GPU'
  },
  {
    category: 'gpu',
    label: 'RTX 3080 Ti',
    query: 'RTX 3080 Ti',
    description: 'Faster 3080-class card'
  },
  {
    category: 'gpu',
    label: 'RTX 3070',
    query: 'RTX 3070',
    description: 'Strong 1440p GPU'
  },
  {
    category: 'gpu',
    label: 'RTX 2080',
    query: 'RTX 2080',
    description: 'Older high-end NVIDIA GPU'
  },
  {
    category: 'gpu',
    label: 'RTX 3060 Ti',
    query: 'RTX 3060 Ti',
    description: 'Good budget 1080p / 1440p card'
  },

  // CPUs
  {
    category: 'cpu',
    label: 'Ryzen 5 5600X',
    query: 'Ryzen 5 5600X',
    description: 'AM4 gaming CPU'
  },
  {
    category: 'cpu',
    label: 'Ryzen 7 5800X',
    query: 'Ryzen 7 5800X',
    description: '8-core AM4 CPU'
  },
  {
    category: 'cpu',
    label: 'Ryzen 5 7600',
    query: 'Ryzen 5 7600',
    description: 'AM5 gaming CPU'
  },
  {
    category: 'cpu',
    label: 'Intel Core i5-12400F',
    query: 'Intel Core i5-12400F',
    description: 'Budget Intel gaming CPU'
  },
  {
    category: 'cpu',
    label: 'Intel Core i7-12700K',
    query: 'Intel Core i7-12700K',
    description: 'High-performance Intel CPU'
  },

  // RAM
  {
    category: 'ram',
    label: '16GB DDR4 3200MHz',
    query: '16GB DDR4 3200MHz RAM',
    description: 'Basic DDR4 gaming memory'
  },
  {
    category: 'ram',
    label: '32GB DDR4 3600MHz',
    query: '32GB DDR4 3600MHz RAM',
    description: 'Good AM4 DDR4 kit'
  },
  {
    category: 'ram',
    label: '32GB DDR5 6000MHz',
    query: '32GB DDR5 6000MHz RAM',
    description: 'Good AM5 / modern Intel DDR5 kit'
  },

  // PSU
  {
    category: 'psu',
    label: '650W Gold PSU',
    query: '650W Gold PSU',
    description: 'For mid-range builds'
  },
  {
    category: 'psu',
    label: '750W Gold PSU',
    query: '750W Gold PSU',
    description: 'Good for most gaming PCs'
  },
  {
    category: 'psu',
    label: '850W Gold PSU',
    query: '850W Gold PSU',
    description: 'For higher-end GPU builds'
  },

  // Motherboards
  {
    category: 'motherboard',
    label: 'B550 AM4 Motherboard',
    query: 'B550 AM4 motherboard',
    description: 'Ryzen 3000/5000 DDR4 board'
  },
  {
    category: 'motherboard',
    label: 'B650 AM5 Motherboard',
    query: 'B650 AM5 motherboard',
    description: 'Ryzen 7000/9000 DDR5 board'
  },
  {
    category: 'motherboard',
    label: 'Z790 LGA1700 Motherboard',
    query: 'Z790 LGA1700 motherboard',
    description: 'Intel 12th/13th/14th gen board'
  }
];