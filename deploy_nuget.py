#!/usr/bin/env python3
# -*- coding: utf-8 -*-

import asyncio
import os
import re
import sys
from typing import Tuple


class PackageInfo:
    def __init__(self):
        self.id = "未知"
        self.authors = "未知"
        self.company = "未知"
        self.title = "未知"
        self.description = "未知"
        self.copyright = "未知"
        self.project_url = "未知"
        self.repository_url = "未知"
        self.tags = "未知"
        self.license = "未知"


async def get_current_version(csproj_path: str) -> Tuple[str, bool]:
    """获取当前版本号，返回版本号和是否成功"""
    try:
        if not os.path.exists(csproj_path):
            print(f"错误: 找不到项目文件: {csproj_path}")
            return "未知", False

        with open(csproj_path, 'r', encoding='utf-8') as file:
            content = file.read()

        match = re.search(r'<Version>(.*?)</Version>', content)
        if match:
            return match.group(1), True
        else:
            print(f"警告: 在项目文件 {csproj_path} 中找不到版本号")
            return "未知", False
    except Exception as ex:
        print(f"读取当前版本号时发生错误: {ex}")

    return "未知", False


async def get_package_info(csproj_path: str) -> PackageInfo:
    """获取NuGet包的所有信息"""
    try:
        with open(csproj_path, 'r', encoding='utf-8') as file:
            content = file.read()

        package_info = PackageInfo()

        id_match = re.search(r'<PackageId>(.*?)</PackageId>', content)
        if id_match:
            package_info.id = id_match.group(1)

        authors_match = re.search(r'<Authors>(.*?)</Authors>', content)
        if authors_match:
            package_info.authors = authors_match.group(1)

        company_match = re.search(r'<Company>(.*?)</Company>', content)
        if company_match:
            package_info.company = company_match.group(1)

        title_match = re.search(r'<Title>(.*?)</Title>', content)
        if title_match:
            package_info.title = title_match.group(1)

        description_match = re.search(r'<Description>(.*?)</Description>', content)
        if description_match:
            package_info.description = description_match.group(1)

        copyright_match = re.search(r'<Copyright>(.*?)</Copyright>', content)
        if copyright_match:
            package_info.copyright = copyright_match.group(1)

        project_url_match = re.search(r'<PackageProjectUrl>(.*?)</PackageProjectUrl>', content)
        if project_url_match:
            package_info.project_url = project_url_match.group(1)

        repository_url_match = re.search(r'<RepositoryUrl>(.*?)</RepositoryUrl>', content)
        if repository_url_match:
            package_info.repository_url = repository_url_match.group(1)

        tags_match = re.search(r'<PackageTags>(.*?)</PackageTags>', content)
        if tags_match:
            package_info.tags = tags_match.group(1)

        license_match = re.search(r'<PackageLicenseExpression>(.*?)</PackageLicenseExpression>', content)
        if license_match:
            package_info.license = license_match.group(1)

        return package_info
    except Exception as ex:
        print(f"读取包信息时发生错误: {ex}")
        return PackageInfo()


async def update_csproj_file(csproj_path: str, version: str, release_notes: str) -> None:
    """更新项目文件"""
    print("正在更新项目文件...")

    with open(csproj_path, 'r', encoding='utf-8') as file:
        content = file.read()

    # 更新版本号
    content = re.sub(r'<Version>.*</Version>', f'<Version>{version}</Version>', content)

    # 更新发布说明
    content = re.sub(r'<PackageReleaseNotes>.*</PackageReleaseNotes>',
                    f'<PackageReleaseNotes>{release_notes}</PackageReleaseNotes>', content)

    with open(csproj_path, 'w', encoding='utf-8') as file:
        file.write(content)

    print("项目文件更新完成。")


async def run_command(command: str) -> int:
    """运行命令并返回退出码"""
    try:
        print(f"执行命令: {command}")

        process = await asyncio.create_subprocess_shell(
            command,
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE,
        )

        stdout, stderr = await process.communicate()

        if stdout:
            print(stdout.decode('utf-8'))

        if stderr:
            print(f"错误: {stderr.decode('utf-8')}")

        return process.returncode
    except Exception as ex:
        print(f"运行命令时发生错误: {ex}")
        return 1


async def build_test_pack_and_publish(version: str) -> int:
    """执行构建、测试、打包和发布流程"""
    print("开始执行构建、测试、打包和发布流程...")

    script_dir = os.path.dirname(os.path.abspath(__file__))
    csproj_path = os.path.join(script_dir, "AvalonLog.csproj")

    # 1. 清理之前的构建
    print("1. 清理之前的构建...")
    if await run_command("dotnet clean --configuration Release") != 0:
        print("清理失败!")
        return 1

    # 2. 恢复依赖项
    print("2. 恢复依赖项...")
    if await run_command("dotnet restore") != 0:
        print("依赖项恢复失败!")
        return 1

    # 3. 构建项目
    print("3. 构建项目...")
    if await run_command(f"dotnet build \"{csproj_path}\" --configuration Release") != 0:
        print("构建失败!")
        return 1

    # 4. 打包项目
    print("4. 打包项目...")
    if await run_command(f"dotnet pack \"{csproj_path}\" --configuration Release --output nupkg --no-build") != 0:
        print("打包失败!")
        return 1

    # 5. 查找生成的包文件
    print("5. 查找生成的包文件...")
    nupkg_directory = os.path.join(script_dir, "nupkg")
    if not os.path.exists(nupkg_directory):
        print("错误: 未找到 nupkg 目录。")
        return 1

    # 查找主包文件
    package_file = None
    for f in os.listdir(nupkg_directory):
        if f.endswith('.nupkg') and not f.endswith('.snupkg') and version in f:
            package_file = os.path.join(nupkg_directory, f)
            print(f"找到主包文件: {package_file}")
            break

    if not package_file:
        print(f"错误: 未找到版本 {version} 对应的 NuGet 包文件。")
        return 1

    # 查找符号包文件
    symbol_package_file = None
    for f in os.listdir(nupkg_directory):
        if f.endswith('.snupkg') and version in f:
            symbol_package_file = os.path.join(nupkg_directory, f)
            print(f"找到符号包文件: {symbol_package_file}")
            break

    if not symbol_package_file:
        print(f"警告: 未找到版本 {version} 对应的符号包文件。")

    # 6. 发布到 NuGet
    print("6. 发布到 NuGet...")
    nuget_source = "https://api.nuget.org/v3/index.json"
    api_key = os.environ.get("NUGET_API_KEY", "")

    if not api_key:
        print("错误: 未设置 NuGet API 密钥环境变量 NUGET_API_KEY。")
        return 1

    # 发布主包
    print("发布包...")
    result = await run_command(f"dotnet nuget push \"{package_file}\" --api-key {api_key} --source {nuget_source}")

    if result != 0:
        print("NuGet 包发布失败!")
        return result

    print(f"NuGet 包 {version} 发布成功!")

    return result


async def main():
    """主函数"""
    print("=== AvalonLog NuGet 包发布工具 ===")

    script_dir = os.path.dirname(os.path.abspath(__file__))
    csproj_path = os.path.join(script_dir, "AvalonLog.csproj")

    # 读取当前版本号
    current_version, success = await get_current_version(csproj_path)
    print(f"当前版本号: {current_version}")

    if not success:
        print("无法读取当前版本号，程序退出。")
        return 1

    version = input("请输入新版本号: ")

    if not version.strip():
        print("错误: 版本号不能为空。")
        return 1

    release_notes = input("请输入版本描述信息: ")

    if not release_notes.strip():
        print("错误: 版本描述信息不能为空。")
        return 1

    # 显示发布预览信息
    print("\n==== 发布预览 ====")
    package_info = await get_package_info(csproj_path)
    print(f"包ID: {package_info.id}")
    print(f"版本: {version}")
    print(f"作者: {package_info.authors}")
    print(f"公司: {package_info.company}")
    print(f"标题: {package_info.title}")
    print(f"描述: {package_info.description}")
    print(f"版权: {package_info.copyright}")
    print(f"项目URL: {package_info.project_url}")
    print(f"仓库URL: {package_info.repository_url}")
    print(f"标签: {package_info.tags}")
    print(f"许可证: {package_info.license}")
    print(f"版本描述: {release_notes}")

    # 显示将要发布的文件路径
    nupkg_directory = os.path.join(script_dir, "nupkg")
    package_file_name = f"Himmelt.AvalonLog.{version}.nupkg"
    symbol_package_file_name = f"Himmelt.AvalonLog.{version}.snupkg"
    package_file_path = os.path.abspath(os.path.join(nupkg_directory, package_file_name))
    symbol_package_file_path = os.path.abspath(os.path.join(nupkg_directory, symbol_package_file_name))
    print(f"主包文件路径: {package_file_path}")
    print(f"符号包文件路径: {symbol_package_file_path}")

    print("==================")

    confirm = input("确认发布? (y/N): ")

    if confirm.lower() != 'y':
        print("发布已取消。")
        return 0

    try:
        # 更新 csproj 文件
        await update_csproj_file(csproj_path, version, release_notes)

        # 执行构建、打包、发布
        result = await build_test_pack_and_publish(version)

        if result == 0:
            print("\nNuGet 包发布成功!")
            return 0
        else:
            print("\nNuGet 包发布失败!")
            return result
    except Exception as ex:
        print(f"发生错误: {ex}")
        return 1


if __name__ == "__main__":
    sys.exit(asyncio.run(main()))
