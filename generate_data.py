import json
import random
import os

# Configuration
OUTPUT_DIR = "data/semantic"
NUM_DOCUMENTS = 100
REGIONS = [f"Region{i:02d}" for i in range(1, 21)]  # Region01 to Region20

# Data Generators
ASSET_TYPES = ["Compressor", "Pump", "Turbine", "Heat Exchanger", "Separator", "Valve"]
MANUFACTURERS = ["Siemens", "GE", "Atlas Copco", "Ingersoll Rand", "Mitsubishi"]
LOCATIONS = ["North Platform", "South Plant", "Offshore Unit 1", "Refinery B", "Pipeline Station 4"]

def generate_id(index):
    return f"doc-{index:03d}"

def generate_name(index, asset_type):
    return f"{asset_type} {random.choice(string_chars)}{index:03d}"

string_chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

def generate_description(name, asset_type, region, manufacturer):
    templates = [
        f"A high-efficiency {asset_type.lower()} manufactured by {manufacturer}. Located in {region}.",
        f"{manufacturer} {asset_type} unit operating in {region}. Critical for process flow.",
        f"Standard {asset_type.lower()} for auxiliary support in {region}. Maintained by {manufacturer}.",
        f"Heavy-duty {asset_type} designed for extreme conditions in {region}.",
        f"Backup {asset_type.lower()} unit, model X-2000, located in {region} sector."
    ]
    return random.choice(templates)

def generate_tags():
    tags = []
    num_tags = random.randint(1, 5)
    for i in range(num_tags):
        tag_name = f"TAG-{random.randint(1000, 9999)}"
        tags.append({
            "name": tag_name,
            "alias": f"Alias_{tag_name}",
            "description": f"Measurement tag for {tag_name}",
            "source": "SCADA",
            "tagtype": random.choice(["Analog", "Discrete"]),
            "engunit": random.choice(["C", "Bar", "RPM", "m3/h"]),
            "dimension": "1"
        })
    return tags

def generate_streams():
    streams = []
    num_streams = random.randint(0, 3)
    for i in range(num_streams):
        stream_name = f"Stream-{random.randint(100, 999)}"
        streams.append({
            "name": stream_name,
            "propertyname": "Value",
            "description": f"Real-time stream for {stream_name}",
            "alias": f"S_{stream_name}",
            "datahubstreamid": f"dh-{random.randint(10000, 99999)}",
            "source": "PI System",
            "tagtype": "Float",
            "engunit": "various",
            "dimension": "1"
        })
    return streams

def generate_metadata(manufacturer, location):
    return [
        { "propertyname": "Manufacturer", "propertyvalue": manufacturer },
        { "propertyname": "Location", "propertyvalue": location },
        { "propertyname": "InstallationDate", "propertyvalue": f"20{random.randint(10, 23)}-01-15" }
    ]

def main():
    # Ensure output directory exists
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    
    print(f"Generating {NUM_DOCUMENTS} documents in {OUTPUT_DIR} with regions {REGIONS[0]} to {REGIONS[-1]}...")

    for i in range(1, NUM_DOCUMENTS + 1):
        asset_type = random.choice(ASSET_TYPES)
        manufacturer = random.choice(MANUFACTURERS)
        location = random.choice(LOCATIONS)
        region = random.choice(REGIONS)
        name = generate_name(i, asset_type)
        
        doc = {
            "id": generate_id(i),
            "name": name,
            "assettypes": [asset_type, "Equipment"],
            "description": generate_description(name, asset_type, region, manufacturer),
            "region": region,
            "tags": generate_tags(),
            "streams": generate_streams(),
            "metadata": generate_metadata(manufacturer, location),
            "@search.action": "upload"
        }
        
        # Write individual file
        filename = f"asset-{i:03d}.json"
        filepath = os.path.join(OUTPUT_DIR, filename)
        with open(filepath, "w") as f:
            json.dump(doc, f, indent=2)

    print(f"Successfully generated {NUM_DOCUMENTS} individual JSON files in {OUTPUT_DIR}")

if __name__ == "__main__":
    main()
