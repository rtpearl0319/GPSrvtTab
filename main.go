package main

import (
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os"
)

const (
	owner = "YOUR_REPO_OWNER"
	repo  = "YOUR_REPO_NAME"
)

type Release struct {
	TagName string `json:"tag_name"`
	Assets  []struct {
		Name               string `json:"name"`
		BrowserDownloadURL string `json:"browser_download_url"`
	} `json:"assets"`
}

func main() {
	// GitHub API URL to get the latest release
	apiURL := fmt.Sprintf("https://api.github.com/repos/%s/%s/releases/latest", owner, repo)

	// Fetch the latest release data
	resp, err := http.Get(apiURL)
	if err != nil {
		fmt.Printf("Error fetching latest release: %v\n", err)
		return
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		fmt.Printf("Error: received non-OK HTTP status %d\n", resp.StatusCode)
		return
	}

	// Decode the JSON response
	var release Release
	if err := json.NewDecoder(resp.Body).Decode(&release); err != nil {
		fmt.Printf("Error decoding JSON: %v\n", err)
		return
	}

	// Find the DLL asset
	var dllAsset string
	for _, asset := range release.Assets {
		if asset.Name == "YOUR_DLL_FILE_NAME.dll" { // Replace with actual DLL file name
			dllAsset = asset.BrowserDownloadURL
			break
		}
	}

	if dllAsset == "" {
		fmt.Println("DLL file not found in latest release")
		return
	}

	// Download the DLL file
	err = downloadFile("latest_release.dll", dllAsset)
	if err != nil {
		fmt.Printf("Error downloading DLL: %v\n", err)
		return
	}

	fmt.Println("DLL downloaded successfully")
}

// Helper function to download a file
func downloadFile(filepath string, url string) error {
	// Create the file
	out, err := os.Create(filepath)
	if err != nil {
		return err
	}
	defer out.Close()

	// Get the data
	resp, err := http.Get(url)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	// Check server response
	if resp.StatusCode != http.StatusOK {
		return fmt.Errorf("error downloading file: %s", resp.Status)
	}

	// Write the body to file
	_, err = io.Copy(out, resp.Body)
	return err
}
